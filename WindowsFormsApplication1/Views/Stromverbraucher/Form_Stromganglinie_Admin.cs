using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    partial class Form_Stromganglinie_Admin : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        public List<StromganglinieModel> DateiListe = new List<StromganglinieModel>();
        string filename = "";
        string filebasename = "";
        string szAppDataPath = "";

        public Form_Stromganglinie_Admin ()
        {
            InitializeComponent();

            szAppDataPath = Path.Combine(Program.ApplicationPath_User, "Strom");
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;  
            Close();
        }

        public void SetControls()
        {
            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            ctrl.ReadAll();

            listBox_Extern.Items.Clear();
            
            for(int i=0; i<ctrl.rows;i++)
            {
                StromganglinieModel model = new StromganglinieModel(); 

                model.m_szBezeichner = ctrl.items[i].m_szBezeichner;
                listBox_Extern.Items.Add(model.m_szBezeichner);
                DateiListe.Add(model);
            }

            szAppDataPath = Path.Combine(Program.ApplicationPath_User, "Strom");
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            StromganglinieStammCtrl ctrl_ganglinie = new StromganglinieStammCtrl();

            // Schreibgeschuetzte (ReadOnly) Stammdatensaetze duerfen nicht geloescht werden.
            if (ctrl_ganglinie.IsReadOnly(listBox_Extern.Text))
            {
                MessageBox.Show("Diese Stromganglinie ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.", "Hinweis");
                return;
            }

            ctrl_ganglinie.Delete(listBox_Extern.Text);
            SetControls(); 
        }

        // Lastgangimport (AP5, Fachkonzept 3.2). Ersetzt den frueheren Weg
        // ".txt, ein Wert je Zeile, harte Anzahlpruefung, Abbruch-MessageBox" durch die
        // Kette Leseschicht -> Pruefung -> Protokoll -> unveraenderte Ablage:
        //
        //   GanglinienDatei.Erkenne   Trennzeichen/Dezimaltrenner/Kopfzeile/Spalten
        //   Form_GanglinieImportOptionen  Vorbelegung anzeigen und uebersteuern lassen
        //   GanglinienDatei.Lies      CSV/TXT ueber NReco, Excel als ein Bulk-Read
        //   GanglinienPruefung.Pruefe Raster, Einheit, Schaltjahr, Sommerzeit, Plausibilitaet
        //   Form_GanglinieProtokoll   Anzeige; Fehler blockieren, Eingriffe brauchen Bestaetigung
        //   DublettenPruefung/Form_ImportKonflikte   Namensabgleich gegen den Katalog (Konzept 4.1)
        //   StromganglinieStammCtrl.ImportGanglinie/ErsetzeGanglinie  Ablage (eine Transaktion)
        //
        // Die Kopie der Quelldatei im Anwenderordner "Strom" bleibt die verlustfreie
        // Originalablage - die Datenbank fuehrt nur die normalisierte Reihe.
        private void btn_Einlesen_Click(object sender, EventArgs e)
        {
            StromganglinieStammCtrl ctrl_stamm = new StromganglinieStammCtrl();

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = szAppDataPath;
            openFileDialog.Filter = MyResource.Resource.IMPORT_DATEIFILTER;
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filename = openFileDialog.FileName;
                filebasename = System.IO.Path.GetFileName(filename);

                try
                {
                    string szQuelle = Path.Combine(szAppDataPath, filebasename);
                    if (!File.Exists(szQuelle))
                    {
                        Directory.CreateDirectory(szAppDataPath);
                        File.Copy(filename, szQuelle, true);
                    }
                }
                catch { }
            }
            openFileDialog = null;

            if (filebasename == "" || filebasename == null ) return;

            // Bezeichner = Dateiname ohne Erweiterung. Die fruehere Vorab-Pruefung
            // gegen die ListBox entfaellt: Ob der Name im Katalog schon vergeben ist,
            // klaert nach dem Lesen/Pruefen die DB-gestuetzte Dublettenpruefung mit
            // Konfliktdialog (Schritt 6) - erst dann stehen Zeitinterval und Werte
            // fuer ein Ueberschreiben bereit.
            string szBezeichner = Path.GetFileNameWithoutExtension(filebasename);

            // Ab hier wird die Kopie im Anwenderordner gelesen, wenn sie existiert.
            string szPfad = Path.Combine(szAppDataPath, filebasename);
            if (!File.Exists(szPfad)) szPfad = filename;

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

            // Vorbelegung des Rasters aus der Auswahlliste des Dialogs (Index statt
            // Anzeigetext - die Beschriftungen sind uebersetzt, die Reihenfolge nicht).
            vorschau.Vorschlag.Raster = RasterAusAuswahl();

            // 2) Optionen bestaetigen oder uebersteuern
            GanglinienImportOptionen optionen;
            using (Form_GanglinieImportOptionen dlg = new Form_GanglinieImportOptionen(szPfad, vorschau))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                optionen = dlg.Optionen;
            }

            // 3) Datei lesen und 4) pruefen
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

            // 6) DB-gestuetzte Dublettenpruefung (Konzept 4.1) - Einzelimport, daher
            // eine Ein-Element-Liste. Sie laeuft bewusst erst NACH dem Lesen/Pruefen,
            // damit fuer "Ueberschreiben" Zeitinterval und Werte bereitstehen.
            KatalogDefinition k = KatalogRegistry.Finde("STROMGANGLINIE");
            ImportKandidat kandidat = new ImportKandidat { Name = szBezeichner };
            kandidat.Werte["Zeitinterval"] = ergebnis.Zeitinterval;
            List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                k, new List<ImportKandidat> { kandidat });

            string szZielName = szBezeichner;
            bool ueberschreiben = false;

            if (pruefungen.Count > 0 && pruefungen[0].Befund != ImportBefund.Neu)
            {
                // EIN Konfliktdialog (eine Zeile) statt der frueheren Abbruch-Meldung.
                // "Importieren" bietet der Dialog bei Namenskonflikt selbst nicht an.
                List<KonfliktEntscheidung> entscheidungen = Form_ImportKonflikte.Zeigen(
                    this, pruefungen, DublettenPruefung.VergebeneNamen(k));
                if (entscheidungen == null) return;   // Abbruch: stiller Ausstieg

                KonfliktEntscheidung ent = entscheidungen[0];
                switch (ent.Aktion)
                {
                    case KonfliktAktion.Auslassen:
                        // Meldung wie der fruehere Duplikat-Abbruch des Bestands.
                        MessageBox.Show(MyResource.Resource.IMPORT_MSG_BEREITS_VORHANDEN,
                                        MyResource.Resource.IMPORT_MSG_HINWEIS,
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    case KonfliktAktion.Ueberschreiben:
                        ueberschreiben = true;
                        break;
                    case KonfliktAktion.Umbenennen:
                        szZielName = ent.NeuerName;
                        break;
                    default:
                        // Importieren (nur bei Befund InhaltsGleich waehlbar):
                        // normaler Neuimport unter dem Originalnamen.
                        break;
                }
            }

            // 7) Ablage - unveraendertes Transaktionsmuster des Bestands
            Cursor.Current = Cursors.WaitCursor;
            bool success;
            try
            {
                success = ueberschreiben
                    ? ctrl_stamm.ErsetzeGanglinie(szZielName, ergebnis.Zeitinterval, ergebnis.Werte)
                    : ctrl_stamm.ImportGanglinie(szZielName, ergebnis.Zeitinterval, ergebnis.Werte);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }

            if (!success)
            {
                MessageBox.Show(MyResource.Resource.IMPORT_MSG_FEHLER_SPEICHERN,
                                MyResource.Resource.IMPORT_MSG_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.IMPORT_MSG_ERFOLG,
                                    szZielName, ergebnis.Werte.Length, ergebnis.Zeitinterval),
                                MyResource.Resource.IMPORT_MSG_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            SetControls();
        }

        // Vorbelegung des Rasters aus comboBox_Zeitinterval. Bewertet wird der Index,
        // nicht der Anzeigetext: die Eintraege sind lokalisiert (de "Stundenwerte" /
        // en "Hourly values"), ihre Reihenfolge ist in beiden Satellitendateien gleich.
        private GanglinienRaster RasterAusAuswahl()
        {
            switch (comboBox_Zeitinterval.SelectedIndex)
            {
                case 0: return GanglinienRaster.Stunde;
                case 1: return GanglinienRaster.Viertelstunde;
                case 2: return GanglinienRaster.Minute;
                default: return GanglinienRaster.Unbekannt;
            }
        }
    }
}