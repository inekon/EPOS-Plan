using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_WP_einlesen : Form
    {
        WaermepumpenImport ctrl = new WaermepumpenImport();

        // Zuordnung: Position in der (gefilterten) ListBox -> Index in ctrl._list.
        // Ersetzt die fruehere Suche ueber Liste_WP.Text, die bei gleichnamigen
        // Eintraegen den falschen Datensatz treffen konnte.
        private List<int> _anzeigeIndex = new List<int>();

        // Sperre gegen Rueckkopplung: waehrend FuelleListe() die Markierung
        // wiederherstellt, feuert SelectedIndexChanged und wuerde die Detailfelder
        // auf einen Zwischenstand setzen.
        private bool _listeWirdGefuellt = false;

        public Form_WP_einlesen()
        {
            InitializeComponent();
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_VDI3805_Click(object sender, EventArgs e)
        {
            string filename = "";

            Liste_WP.Items.Clear();

            string szAppDataPath = Path.Combine(Properties.Settings.Default.VDI3805Path, "VDI");

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

            int sel = Liste_WP.SelectedIndex;
            if (sel < 0 || sel >= _anzeigeIndex.Count) return;
            ZeigeDetails(_anzeigeIndex[sel]);
        }

        // Uebertraegt einen VDI-Eintrag in die Detailfelder.
        private void ZeigeDetails(int i)
        {
            if (i < 0 || i >= ctrl._list.Count) return;

            textBox_Name.Text = ctrl._list[i].szName;
            textBox_Firma.Text = ctrl._list[i].szFirma;
            textBox_Typ.Text = ctrl._list[i].szWPTyp;
            textBox_Zusatzheizung.Text = ctrl._list[i].szElektrZuheizung;
            textBox_ThLeistung.Text = ctrl._list[i].szThLeistung;
            textBox_Aufstellung.Text = ctrl._list[i].szAufstellung;
            textBox_Stufen.Text = ctrl._list[i].szStufen;
            textBox_MaxVorlauf.Text = ctrl._list[i].szMaxVorlauf;
            textBox__Wirkungsgrad.Text = ctrl._list[i].szCOP;
            textBox_Kuehlleistung.Text = ctrl._list[i].szKuehlleistung;
        }

        private void Leistungsfilter_ValueChanged(object sender, EventArgs e)
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
            double min = (double)num_LeistungVon.Value;
            double max = (double)num_LeistungBis.Value;
            string suche = txt_Filter.Text;

            // Markierung (echte Indizes) sichern und nach dem Neuaufbau fuer alle
            // weiterhin sichtbaren Eintraege wiederherstellen: so geht eine bereits
            // getroffene Auswahl beim Tippen im Filter nicht stillschweigend
            // verloren, und unsichtbare Eintraege bleiben unmarkiert.
            List<int> markiert = MarkierteQuellIndizes();

            _listeWirdGefuellt = true;
            Liste_WP.BeginUpdate();
            Liste_WP.Items.Clear();
            _anzeigeIndex.Clear();
            for (int i = 0; i < ctrl._list.Count; i++)
            {
                // ZahlParsen statt convertTxt2Double: eine nicht parsbare
                // Leistungsangabe darf den Listenaufbau nicht abbrechen - sie
                // zaehlt als 0, den Fehler meldet erst die Uebernahme.
                double leistung;
                if (!Program.ZahlParsen(ctrl._list[i].szThLeistung, out leistung)) leistung = 0;
                if (leistung < min || leistung > max) continue;
                if (!VdiAuswahlFilter.Passt(suche, ctrl._list[i].szName, ctrl._list[i].szFirma)) continue;
                Liste_WP.Items.Add(ctrl._list[i].szName);
                _anzeigeIndex.Add(i);
            }
            for (int zeile = 0; zeile < _anzeigeIndex.Count; zeile++)
            {
                if (markiert.Contains(_anzeigeIndex[zeile])) Liste_WP.SetSelected(zeile, true);
            }
            Liste_WP.EndUpdate();
            _listeWirdGefuellt = false;

            // Detailfelder auf die verbleibende Markierung nachziehen, damit sie
            // nach dem Umfiltern nicht mehr auf einen ausgefilterten Eintrag
            // zeigen (bei der Uebernahme sind sie die Quelle bzw. die Anzeige).
            if (Liste_WP.SelectedIndex >= 0 && Liste_WP.SelectedIndex < _anzeigeIndex.Count)
                ZeigeDetails(_anzeigeIndex[Liste_WP.SelectedIndex]);
        }

        // Markierte Zeilen -> Indizes in ctrl._list.
        private List<int> MarkierteQuellIndizes()
        {
            return VdiAuswahlFilter.QuellIndizes(Liste_WP.SelectedIndices, _anzeigeIndex);
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            // Mehrfachselektion: je markierter Eintrag laeuft genau der bestehende
            // Einzel-Weg (UebernehmeEintrag) - nur in einer Schleife, damit es
            // keinen zweiten Schreibpfad in die STAMM-Tabellen gibt.
            List<int> markiert = MarkierteQuellIndizes();
            if (markiert.Count == 0) return;

            // Vorpruefung der gesamten Auswahl gegen den Katalog und gegen sich
            // selbst (Konzept 4.1) - noch ohne Schreibzugriff.
            KatalogDefinition katalog = KatalogRegistry.Finde("WP");
            List<ImportKandidat> kandidaten = new List<ImportKandidat>();
            foreach (int i in markiert)
            {
                WPStammCtrl probe = new WPStammCtrl();
                FuelleModellwerte(i, probe);
                ImportKandidat kand = new ImportKandidat { Name = probe.WPName, Tag = i };
                kand.Werte["Firma"] = probe.Firma;
                kand.Werte["Typ"] = probe.Typ;
                kand.Werte["Baujahr"] = probe.Baujahr;
                kand.Werte["Aufstellung"] = probe.Aufstellung;
                kand.Werte["Nennleistung"] = probe.Nennleistung;
                kand.Werte["maxPtherm"] = probe.maxPTherm;
                kand.Werte["Heizung"] = probe.Heizung;
                kand.Werte["Regelung"] = probe.Regelung;
                kand.Werte["Bauart"] = probe.Bauart;
                kand.Werte["Kuehlleistung"] = probe.Kuehlleistung;
                kandidaten.Add(kand);
            }

            List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(katalog, kandidaten);

            bool konflikt = false;
            foreach (ImportPruefung p in pruefungen)
                if (p.Befund != ImportBefund.Neu || p.NameDoppeltInAuswahl) { konflikt = true; break; }

            if (!konflikt && markiert.Count == 1)
            {
                // Konfliktfreier Einzelfall: Meldungen bleiben wortgleich beim Bestand.
                VdiUebernahmeErgebnis einzel = UebernehmeEintrag(markiert[0]);
                if (einzel == VdiUebernahmeErgebnis.Duplikat) MessageBox.Show("Daten bereits eingelesen!");
                else if (einzel == VdiUebernahmeErgebnis.Gespeichert) MessageBox.Show("Daten gespeichert!");
                return;
            }

            List<KonfliktEntscheidung> entscheidungen;
            if (konflikt)
            {
                // EIN Dialog fuer die ganze Auswahl statt einer Meldung je Satz.
                entscheidungen = Form_ImportKonflikte.Zeigen(this, pruefungen,
                    DublettenPruefung.VergebeneNamen(katalog));
                if (entscheidungen == null) return;
            }
            else
            {
                entscheidungen = new List<KonfliktEntscheidung>();
                foreach (ImportPruefung p in pruefungen)
                    entscheidungen.Add(new KonfliktEntscheidung { Pruefung = p, Aktion = KonfliktAktion.Importieren });
            }

            FuehreAus(markiert.Count, entscheidungen);
        }

        // Fuehrt die im Konfliktdialog gewaehlten Aktionen aus und zeigt die
        // Sammelmeldung. Auslassen zaehlt als uebersprungen.
        private void FuehreAus(int markiertAnzahl, List<KonfliktEntscheidung> entscheidungen)
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

                    // Detailfelder je Eintrag mitfuehren, damit die Anzeige zu dem
                    // passt, was gerade geschrieben wird.
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
                                ergebnis = UebernehmeEintrag(i, ent.NeuerName);
                                if (ergebnis == VdiUebernahmeErgebnis.Gespeichert)
                                    ergebnis = VdiUebernahmeErgebnis.Umbenannt;
                                break;
                            default:
                                ergebnis = UebernehmeEintrag(i);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ein fehlerhafter Eintrag darf den Gesamtvorgang nicht abbrechen.
                        Console.WriteLine("Fehler beim Einlesen von '" + ctrl._list[i].szName + "': " + ex.Message);
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
        }

        // Befuellt den Stamm-Controller mit den Importwerten eines Listeneintrags -
        // gemeinsame Quelle fuer Vorpruefung, Neuanlage und Ueberschreiben.
        private void FuelleModellwerte(int index, WPStammCtrl wpctrl)
        {
            wpctrl.WPName = ctrl._list[index].szName;

            int nStufen = Program.convertTxt2Int(ctrl._list[index].szStufen);
            if (nStufen == 0) wpctrl.Regelung = "stetig";
            else if (nStufen == 1) wpctrl.Regelung = "einstufig";
            else if (nStufen == 2) wpctrl.Regelung = "zweistufig";
            else wpctrl.Regelung = "mehrstufig";

            wpctrl.Aufstellung = ctrl._list[index].szAufstellung;
            wpctrl.Firma = ctrl._list[index].szFirma;
            double pd = Program.convertTxt2Double(ctrl._list[index].szThLeistung);
            wpctrl.Nennleistung = (int)pd;
            wpctrl.Typ = ctrl._list[index].szWPTyp;
            wpctrl.Bauart = ctrl._list[index].szBauart;

            if (ctrl._list[index].szElektrZuheizung != "")
            {
                double heizstab = Program.convertTxt2Double(ctrl._list[index].szElektrZuheizung);
                wpctrl.Heizung = (int)heizstab;
                double kuehlung = 0;
                kuehlung = Program.convertTxt2Double(ctrl._list[index].szKuehlleistung);
                wpctrl.Kuehlleistung = kuehlung;
            }
        }

        // Uebernahme genau eines VDI-Eintrags in Tab_WP_STAMM samt Kenndaten.
        // Bestandsweg; nameOverride traegt beim Umbenennen (Konfliktdialog) den
        // vom Anwender vergebenen neuen Bezeichner (Konzept 4.3).
        private VdiUebernahmeErgebnis UebernehmeEintrag(int index, string nameOverride = null)
        {
            WPStammCtrl wpctrl = new WPStammCtrl();

            FuelleModellwerte(index, wpctrl);
            if (nameOverride != null) wpctrl.WPName = nameOverride;

            if (wpctrl.Exists(wpctrl.WPName)) return VdiUebernahmeErgebnis.Duplikat;

            // Aufraeumklammer um den GESAMTEN Schreibvorgang dieses Eintrags. Die
            // Inserts laufen im Controller ueber getrennte Verbindungen, eine
            // gemeinsame Transaktion ist ohne Controller-Umbau nicht moeglich.
            // Deshalb: scheitert irgendein Schritt (false oder Exception), wird ein
            // bereits angelegter Stammsatz samt Kennlinien wieder geloescht, damit
            // kein unvollstaendiger Datensatz stehen bleibt (Befund 17.08.2026).
            // Der Stammsatz-Insert steht bewusst INNERHALB der Klammer: Insert()
            // committet den Satz und liest erst danach @@IDENTITY zurueck - misslingt
            // dieses Rueckmelden, meldet Insert() false, obwohl der Satz schon in der
            // Datenbank steht. Ohne Satz laeuft Delete() ins Leere und stoert nicht.
            bool bVollstaendig = false;
            try
            {
                if(!wpctrl.Insert()) return VdiUebernahmeErgebnis.Fehler;

                List<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenn;
                List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehl;
                SammleKennlinien(index, out kenn, out kuehl);

                foreach (var k in kenn)
                    if (!wpctrl.InsertKenndatenStamm(wpctrl.ID, k.Vorlauf, k.Temperatur, k.COP, k.Ptherm))
                        return VdiUebernahmeErgebnis.Fehler;

                foreach (var k in kuehl)
                    if (!wpctrl.InsertKenndatenKuehlungStamm(wpctrl.ID, k.Vorlauf, k.Temperatur, k.COP, k.Pkuehl, k.Last))
                        return VdiUebernahmeErgebnis.Fehler;

                bVollstaendig = true;
                return VdiUebernahmeErgebnis.Gespeichert;
            }
            finally
            {
                // Delete() loescht per Bezeichner (die Duplikatpruefung oben stellt
                // Eindeutigkeit sicher), faengt eigene Fehler und wirft nie.
                if (!bVollstaendig && !wpctrl.Delete())
                    Console.WriteLine("Unvollstaendiger WP-Stammsatz '" + wpctrl.WPName + "' (ID " + wpctrl.ID + ") konnte nicht aufgeraeumt werden!");
            }
        }

        // Sammelt die Kennlinien eines VDI-Eintrags (Heizen 710.09/1, Kuehlen 710.09/2,
        // Wertzeilen 710.91) als Listen - gemeinsame Quelle fuer Neuanlage und
        // Ueberschreiben. Parse-Logik unveraendert aus dem Bestandsweg uebernommen.
        private void SammleKennlinien(int index,
            out List<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenn,
            out List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehl)
        {
            kenn = new List<(int Vorlauf, int Temperatur, double COP, double Ptherm)>();
            kuehl = new List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)>();

            string vorlauf = "";
            string last = "";
            bool anfang = false;
            bool anfang_kuehl = false;
            List<string> datlines = ctrl._list[index].x;

            for (int i = 0; i < datlines.Count; i++)
            {
                string[] token = datlines[i].Split(';');
                if (token[0] == "710.09" && token[2] == "1")
                {
                    vorlauf = token[3];
                    anfang = true;
                }
                else if (token[0] == "710.09" && token[2] == "2")
                {
                    vorlauf = token[3];
                    anfang_kuehl = true;
                    anfang = false;
                    last = token[7];
                }
                else if (anfang && (token[0] == "710.91"))
                {
                    kenn.Add((Program.convertTxt2Int(vorlauf),
                              Program.convertTxt2Int(token[2]),
                              Program.convertTxt2Double(token[5]),
                              Program.convertTxt2Double(token[3])));
                }
                else if (anfang_kuehl && (token[0] == "710.91"))
                {
                    int nLast = last.ToUpper() == "MAX" ? 100 : Program.convertTxt2Int(last);
                    kuehl.Add((Program.convertTxt2Int(vorlauf),
                               Program.convertTxt2Int(token[2]),
                               Program.convertTxt2Double(token[5]),
                               Program.convertTxt2Double(token[3]),
                               nLast));
                }
            }
        }

        // Ueberschreiben aus dem Konfliktdialog: aktualisiert genau die Importfelder
        // des Bestandssatzes und tauscht die Kennlinien in einer Transaktion -
        // ID, Bezeichner und Anwenderfelder bleiben stehen (Konzept 4.2).
        private VdiUebernahmeErgebnis UeberschreibeEintrag(int index, int bestandsId)
        {
            WPStammCtrl wpctrl = new WPStammCtrl();
            FuelleModellwerte(index, wpctrl);

            List<(int Vorlauf, int Temperatur, double COP, double Ptherm)> kenn;
            List<(int Vorlauf, int Temperatur, double COP, double Pkuehl, int Last)> kuehl;
            SammleKennlinien(index, out kenn, out kuehl);

            return wpctrl.UeberschreibeMitKennlinien(bestandsId, kenn, kuehl)
                ? VdiUebernahmeErgebnis.Ueberschrieben
                : VdiUebernahmeErgebnis.Fehler;
        }

    }
}
