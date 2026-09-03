using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class MenueCtrl
    {
        public WizardParent wizparent;

        public MenueCtrl()
        {
            wizparent = null;
        }

        public void SetProjektname()
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle();
            FormMain frm = (FormMain)Program.mainfrm;
            frm.SetProjekt(ctrl.m_szProjektname);
        }

        /// <summary>
        /// Öffnet den Projektassistenten für ein NEUES Projekt.
        ///
        /// <para>
        /// <b>P4 (Projektdialoge vereinheitlichen): eine Seitenliste statt zwei.</b>
        /// Die dreizehn Zeilen Seitenaufbau standen hier und in
        /// <see cref="ProjektBearbeiten"/> wortgleich doppelt; sie liegen jetzt in
        /// <see cref="AssistentSeiten"/>. Reihenfolge und Inhalt sind unverändert —
        /// die beiden Einstiege unterscheiden sich nur noch im <c>SetWizardMode</c>.
        /// </para>
        /// </summary>
        public void ProjektNeu()
        {
            AssistentZeigen(WizardParent.WIZARD_MODE_NEU);
        }

        /// <summary>
        /// Öffnet den Projektassistenten für ein BESTEHENDES Projekt (linke Spalte =
        /// <see cref="ProjektAuswahl"/>). Seitenliste wie in <see cref="ProjektNeu"/>.
        /// </summary>
        public void ProjektBearbeiten()
        {
            AssistentZeigen(WizardParent.WIZARD_MODE_BEARBEITEN);
        }

        private void AssistentZeigen(int betriebsart)
        {
            wizparent = new WizardParent(AssistentSeiten.Erzeugen());
            Program.wizardctrl.parentform = wizparent;
            wizparent.SetWizardMode(betriebsart);
            wizparent.ShowDialog();

            if (wizparent.gespeichert)
            {
                MessageBox.Show("Daten gespeichert");
            }
        }

        /// <summary>
        /// Öffnet ein Projekt im Detailformular <see cref="FormMain"/>.
        ///
        /// <para>
        /// <b>P3 (Projektdialoge vereinheitlichen): „Öffnen" öffnet jetzt wirklich.</b>
        /// Bis dahin zeigte dieser Menüweg <see cref="Form_ProjektSpeichernUnter"/>,
        /// verlangte einen NEUEN Projektnamen und DUPLIZIERTE das Projekt; erst danach
        /// wurde das Ausgangsprojekt geöffnet. Duplizieren heißt jetzt ausschließlich
        /// „Speichern unter…"; hier steht die neue <see cref="Form_ProjektAuswahl"/>
        /// (Liste, Suche, Sortierung).
        /// </para>
        /// <para>
        /// <b>Ein Ladeweg statt zwei.</b> Die rund 40 Zeilen Set*/Add_*-Aufrufe standen
        /// zweimal wortgleich hier (Zweig „gewähltes Projekt" und Zweig „zuletzt
        /// geöffnet"). Sie liegen jetzt in <see cref="ProjektInFormMainLaden"/> — damit
        /// entfällt auch der Befund „MenueCtrl:158": dort las der Zweig „zuletzt
        /// geöffnet" <c>frm.m_szProjekt</c> vom NIE ANGEZEIGTEN Speichern-unter-Dialog
        /// und übergab an <c>SetWaermebedarfExternControl</c> garantiert einen leeren
        /// Namen; die Liste „Wärmebedarf einlesen" blieb im Detailformular leer.
        /// </para>
        /// </summary>
        /// <param name="zuletzt">true = ohne Dialog das zuletzt geöffnete Projekt laden.</param>
        public void ProjektOeffnen(bool zuletzt = false)
        {
            if (!zuletzt)
            {
                using (Form_ProjektAuswahl frm = new Form_ProjektAuswahl())
                {
                    if (frm.ShowDialog() != DialogResult.OK) return;
                    if (frm.m_ID_Projekt <= 0 || frm.m_szProjekt == "") return;
                    ProjektInFormMainLaden(frm.m_szProjekt, frm.m_ID_Projekt);
                }
                return;
            }

            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle();
            if (ctrl.m_szProjektname == "") return;
            ProjektInFormMainLaden(ctrl.m_szProjektname, ctrl.m_ID_Projekt);
        }

        /// <summary>
        /// Der EINE Ladeweg ins Detailformular: Stammdaten, alle Listen, alle
        /// Kontextmenüs, Anzeige als Dialog, danach den Projektkontext der Startseite
        /// nachziehen. Inhaltlich unverändert gegenüber den beiden bisherigen Zweigen
        /// von <see cref="ProjektOeffnen"/>; die Klimaregion wird — wie im Zweig
        /// „zuletzt geöffnet" — aus dem Projekt gelesen (der frühere Weg über
        /// <c>Form_ProjektSpeichernUnter.m_szKlimaregion</c> lieferte immer "").
        /// </summary>
        public void ProjektInFormMainLaden(string szProjekt, int idProjekt)
        {
            ProjektCtrl ctrlproj = new ProjektCtrl();
            ctrlproj.ReadSingle(szProjekt);

            Program.mainfrm = new FormMain();
            FormMain frmmain = (FormMain)Program.mainfrm;

            string szKlima = frmmain.GetKlimaregion(ctrlproj.m_ID_Klimaregion);

            frmmain.SetProjekt(szProjekt);
            frmmain.SetIDProjekt(idProjekt);
            frmmain.SetKlima(szKlima);
            Program.startfrm.SetKlima(szKlima);
            frmmain.SetBearbeiter(ctrlproj.m_szBearbeiter);
            frmmain.SetKunde(ctrlproj.m_szKunde);
            frmmain.SetAenderungsdatum(ctrlproj.m_Aenderungsdatum);
            frmmain.SetBeschreibung(ctrlproj.m_szBeschreibung);
            frmmain.SetWPControl(szProjekt);
            frmmain.SetBHKWControl(szProjekt);
            frmmain.SetSPControl(szProjekt);
            frmmain.SetHeizkesselControl(szProjekt);
            frmmain.SetGebaeudeControl(szProjekt);
            frmmain.SetWaermebedarfExternControl(szProjekt);
            frmmain.SetProzesswaermeControl(idProjekt);
            frmmain.SetStrombedarfControl(idProjekt);
            frmmain.SetStromganglinieControl(szProjekt);
            frmmain.SetPVControl(szProjekt);
            frmmain.SetPufferSpControl(szProjekt);
            frmmain.SetSolarControl(szProjekt);
            frmmain.Add_WPKontext();
            frmmain.Add_BHKWKontext();
            frmmain.Add_GebäudeKontext();
            frmmain.Add_HeizkesselKontext();
            frmmain.Add_WaermebedarfExternKontext();
            frmmain.Add_ProzesswaermeKontext();
            frmmain.Add_StrombedarfKontext();
            frmmain.Add_StromganglinieKontext();
            frmmain.Add_SpKontext();
            frmmain.Add_PVKontext();
            frmmain.Add_SolarKontext();

            frmmain.ShowDialog();

            Program.startfrm.m_szProjektname = szProjekt;
            Program.startfrm.m_ID_Projekt = idProjekt;
            Program.startfrm.SetTextProjekt(szProjekt);
        }

        /// <summary>
        /// Macht ein Projekt zum AKTIVEN Projekt der Startmaske — <b>ohne</b> das
        /// Detailformular <see cref="FormMain"/>.
        ///
        /// <para>
        /// <b>Abgrenzung zu <see cref="ProjektInFormMainLaden"/>.</b> Dort ist das
        /// Detailformular „Konfiguration Projekt" der Zweck: Es wird gebaut, mit allen
        /// Listen und Kontextmenüs bestückt und modal gezeigt; der Projektkontext der
        /// Startmaske wird erst nachgezogen, wenn der Anwender es schließt. Diesen Weg
        /// gehen weiterhin das Menü „Projekt → Öffnen…", „Zuletzt geöffnet" und die
        /// Kachel „Projekt Details". Hier dagegen wird das Projekt einfach das aktive:
        /// Startmaske zeigt es, „zuletzt geöffnet" merkt es sich, kein Fenster geht auf
        /// (Nutzerwunsch 30.08.2026 zum Knopf „Projekt öffnen" im Assistenten).
        /// </para>
        /// <para>
        /// <b>Eine Wahrheit.</b> Alles, was die Startmaske nachziehen muss — Name/ID,
        /// Kopfband, Klimaregion, Statuszeichen, Freischaltung der Reiter, Kachelstatus
        /// (Bitmaske) und Variantenanzeige —, steht bereits in
        /// <see cref="Form_Start.ProjektKontextUebernehmen"/>; das Merken in
        /// <c>Tab_Applikation</c> in <see cref="Form_Start.ZuletztGeoeffnetMerken"/>.
        /// Beides wird hier nur AUFGERUFEN, nichts davon nachgebaut. Die Klimaregion
        /// braucht deshalb auch keinen eigenen Leseweg über eine
        /// <see cref="FormMain"/>-Instanz (<c>GetKlimaregion</c>):
        /// <c>ProjektKontextUebernehmen</c> füllt dasselbe Feld, das
        /// <c>Form_Start.SetKlima</c> beschreibt, und liest dafür über
        /// <c>GetProjektKlimaregion</c> die PROJEKTKOPIE der Klimaregion
        /// (<c>Tab_Klimaregion</c>) statt des Stammsatzes.
        /// </para>
        /// </summary>
        /// <param name="szProjekt">Projektname — der führende Schlüssel.</param>
        /// <param name="idProjekt">
        /// Projekt-ID; wird nur als Rückfall benutzt, wenn der Aufrufer keinen Namen hat.
        /// </param>
        /// <returns>
        /// false, wenn es die Startmaske nicht gibt oder zu Name/ID kein Projekt
        /// existiert (z. B. zwischenzeitlich gelöscht). Der Aufrufer erkennt daran, dass
        /// er keine Erfolgsmeldung zeigen darf; der bisherige Kontext bleibt stehen.
        /// </returns>
        public bool ProjektAktivSetzen(string szProjekt, int idProjekt)
        {
            Form_Start start = Program.startfrm;
            if (start == null) return false;

            if (string.IsNullOrWhiteSpace(szProjekt) && idProjekt > 0)
            {
                ProjektCtrl ctrlproj = new ProjektCtrl();
                ctrlproj.ReadSingle(idProjekt);
                szProjekt = ctrlproj.rows > 0 ? ctrlproj.m_szProjektname : "";
            }

            if (!start.ProjektKontextUebernehmen(szProjekt)) return false;

            start.ZuletztGeoeffnetMerken();
            return true;
        }

        /// <summary>
        /// Dupliziert ein Projekt („Speichern unter…") — der Weg, der bis P3
        /// fälschlich hinter dem Menüpunkt „Öffnen…" steckte. Aufrufer ist heute die
        /// Startmasken-Kachel „Speichern unter"; die Methode steht hier, damit der
        /// Duplizierweg einen ehrlichen Namen und eine Menü-Anlaufstelle hat.
        /// </summary>
        /// <returns>true, wenn dupliziert wurde.</returns>
        public bool ProjektSpeichernUnter()
        {
            using (Form_ProjektSpeichernUnter frm = new Form_ProjektSpeichernUnter())
                return frm.ShowDialog() == DialogResult.OK;
        }

        /// <summary>
        /// Projekte löschen (Nutzerauftrag 02.09.2026: Mehrfachauswahl analog zum
        /// Öffnen-Dialog). Der Dialog liefert die angehakten Projekte — Varianten vor
        /// ihren Stämmen — und hat bereits zurückgefragt. Je Projekt läuft der bewährte
        /// Weg (Anlagen, Projektzeile samt Kaskaden) plus die gespeicherten
        /// Ergebnisse, die bisher als Rückstand blieben. Rückgabe wie bisher ein
        /// Name: der des aktiven Projekts, falls es dabei war (Form_Start setzt dann
        /// seinen Platzhalter), sonst der zuletzt gelöschte; leer, wenn nichts geschah.
        /// </summary>
        public string ProjektDelete(bool zuletzt = false)
        {
            List<ProjektModel> liste;
            bool sicherung;
            using (Form_ProjektDelete frm = new Form_ProjektDelete())
            {
                if (frm.ShowDialog() != DialogResult.OK || frm.ZuLoeschen.Count == 0) return "";
                liste = frm.ZuLoeschen;
                sicherung = frm.SicherungGewuenscht;
            }

            if (sicherung && !DatenbankSichern("vor_Loeschen")) return "";

            int aktuell = AktuelleProjektId();
            string aktuellerName = "";
            string letzter = "";
            int n = 0;
            var fehler = new List<string>();
            foreach (ProjektModel p in liste)
            {
                try
                {
                    if (aktuell > 0 && p.m_ID == aktuell)
                    {
                        AktuellesProjektZuruecksetzen();
                        aktuellerName = p.m_szProjektname ?? "";
                    }

                    // Gespeicherte Simulationsergebnisse blieben beim alten Weg als
                    // Rückstand (Tab_Ergebnis hängt an keiner Löschweitergabe).
                    try { new ErgebnisCtrl().Delete(p.m_ID); } catch { }

                    WErzeugerCtrl ctrlwerz = new WErzeugerCtrl();
                    ctrlwerz.ID_Projekt = p.m_ID;
                    ctrlwerz.Delete();

                    ProjektCtrl ctrlproj = new ProjektCtrl();
                    ctrlproj.m_szProjektname = p.m_szProjektname;
                    ctrlproj.Delete(p.m_szProjektname);

                    letzter = p.m_szProjektname ?? "";
                    n++;
                }
                catch (Exception ex)
                {
                    fehler.Add((p.m_szProjektname ?? "?") + ": " + ex.Message);
                }
            }

            string meldung = string.Format(Form_ProjektDelete.TPd("PDLG_ERFOLG", "{0} Projekt(e) gelöscht."), n);
            if (fehler.Count > 0)
                meldung += "\r\n\r\n" + string.Format(Form_ProjektDelete.TPd("PDLG_FEHLER", "Fehler bei:\r\n{0}"),
                                                       string.Join("\r\n", fehler));
            MessageBox.Show(meldung, Form_ProjektDelete.TPd("PDLG_TITEL", "Projekte löschen"), MessageBoxButtons.OK,
                            fehler.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            return aktuellerName.Length > 0 ? aktuellerName : letzter;
        }

        // Das aktive Projekt (Tab_Applikation.ID_Projekt), 0 wenn keins.
        private static int AktuelleProjektId()
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Applikation");
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["ID_Projekt"] != DBNull.Value)
                    return Convert.ToInt32(dt.Rows[0]["ID_Projekt"]);
            }
            catch { }
            return 0;
        }

        // Gezieltes UPDATE statt CommandBuilder (Bestandsweg vor dem Umbau).
        private static void AktuellesProjektZuruecksetzen()
        {
            OleDbParameter pName = new OleDbParameter("?", "");
            DataRepository.ExecuteNonQuery("UPDATE Tab_Applikation SET Projektname = ?, ID_Projekt = 0", pName);
        }

        /// <summary>
        /// Sicherungskopie der aktiven Datenbank — in den Ordner „DB-Backup" neben der
        /// Datei, falls es ihn gibt (der Migrationsstrang legt ihn an), sonst daneben;
        /// Zweck und Zeitstempel im Namen. Bei SQLite reisen die Journal-Dateien
        /// (-wal/-shm) mit, damit die Kopie den letzten Stand vollständig trägt.
        /// Wirft bei Fehlern (der Aufrufer entscheidet, ob er fortfährt).
        /// Gemeinsame Wahrheit für „Projekte löschen" und den Projektimport.
        /// </summary>
        public static string DatenbankKopieAnlegen(string zweck)
        {
            string dbPfad = DataRepository.GetDBPath();
            string ordner = System.IO.Path.GetDirectoryName(dbPfad) ?? "";
            string backupOrdner = System.IO.Path.Combine(ordner, "DB-Backup");
            if (System.IO.Directory.Exists(backupOrdner)) ordner = backupOrdner;
            string stamm = System.IO.Path.GetFileNameWithoutExtension(dbPfad) + "_" + zweck + "_" +
                           DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string sicherung = System.IO.Path.Combine(ordner, stamm + System.IO.Path.GetExtension(dbPfad));
            System.IO.File.Copy(dbPfad, sicherung, false);
            foreach (string anhang in new[] { "-wal", "-shm" })
                if (System.IO.File.Exists(dbPfad + anhang))
                    System.IO.File.Copy(dbPfad + anhang, sicherung + anhang, true);
            return sicherung;
        }

        // Sicherungskopie vor dem Löschen; false = der Anwender möchte nach einem
        // Sicherungsfehler NICHT fortfahren.
        private static bool DatenbankSichern(string zweck)
        {
            try
            {
                DatenbankKopieAnlegen(zweck);
                return true;
            }
            catch (Exception ex)
            {
                return MessageBox.Show(
                    string.Format(Form_ProjektDelete.TPd("PDLG_SICHERUNG_FEHLER",
                        "Die Sicherungskopie konnte nicht angelegt werden:\r\n{0}\r\n\r\nTrotzdem löschen?"), ex.Message),
                    Form_ProjektDelete.TPd("PDLG_TITEL", "Projekte löschen"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            }
        }

        public void WP_Administration()
        {
            Form_WP frm = new Form_WP();
            frm.ShowDialog();
        }

        public void StromspeicherBearbeiten()
        {
            Form_AdminStromspeicher frm = new Form_AdminStromspeicher();
            frm.ShowDialog();
        }

        /// <summary>
        /// Öffnet die Lastspitzenkappung (Peak-Shaving) – eigener Einstieg nach
        /// Fachkonzept 6.4 (AP7). Ein geöffnetes Projekt ist ausdrücklich nicht
        /// nötig: ohne Projekt stehen Stammganglinien und der Direktimport zur
        /// Verfügung, deshalb hier auch keine Projektprüfung.
        /// </summary>
        public void PeakShavingBearbeiten()
        {
            int idProjekt = Program.startfrm != null ? Program.startfrm.m_ID_Projekt : 0;
            using (Form_PeakShaving frm = new Form_PeakShaving(idProjekt))
                frm.ShowDialog();
        }

        public void GebaeudeBearbeiten()
        {
            Form_Gebaeude frm = new Form_Gebaeude();
            frm.m_bAdmin = true;
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void GebaeudetypenBearbeiten()
        {
            Form_EingGebTyp frm = new Form_EingGebTyp();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void WaermebedarfExtern()
        {
            Form_AdminWaermeeinlesen frm = new Form_AdminWaermeeinlesen();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void Prozesswaerme()
        {
            Form_Prozesswaerme_Admin frm = new Form_Prozesswaerme_Admin();
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void Stromverbraucher()
        {
            Form_Stromverbraucher_Admin frm = new Form_Stromverbraucher_Admin();
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void Stromganglinie()
        {
            Form_Stromganglinie_Admin frm = new Form_Stromganglinie_Admin();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void Solarganglinie()
        {
            Form_Solarganglinie_Admin frm = new Form_Solarganglinie_Admin();
            frm.SetControls();
            frm.ShowDialog();
        }

        public void WPImport()
        {
            Form_WP_einlesen frm = new Form_WP_einlesen();
            frm.ShowDialog();
        }

        public void Kessel()
        {
            Form_Heizkessel_Admin frm = new Form_Heizkessel_Admin();
            frm.ShowDialog();
        }

        public void BHKW()
        {
            Form_BHKWAdmin frm = new Form_BHKWAdmin();
            frm.ShowDialog();
        }
        public void Solarkollektoren()
        {
            Form_SolarKollektorenAdmin frm = new Form_SolarKollektorenAdmin();
            frm.ShowDialog();
        }

        public void PV()
        {
            Form_AdminPV frm = new Form_AdminPV();
            frm.ShowDialog();
        }

        public void SPKImport()
        {
            Form_Heizkessel_einlesen frm = new Form_Heizkessel_einlesen();
            frm.ShowDialog();
        }

        public void PufferSPImport()
        {
            Form_PufferSp_einlesen frm = new Form_PufferSp_einlesen();
            frm.ShowDialog();
        }

        public void PufferSp()
        {
            Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
            frm.ShowDialog();
        }

        public void Brauchwasser()
        {
            Form_Brauchwasser_Admin frm = new Form_Brauchwasser_Admin();
            frm.SetControls("");
            frm.ShowDialog();
        }

        public void PVImport()
        {

        }

        public void SolarThermieImport()
        {
            Form_SolarKollektoren_einlesen frm = new Form_SolarKollektoren_einlesen();
            frm.ShowDialog();
        }
    }
}