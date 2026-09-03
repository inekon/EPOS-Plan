using System;
using System.Collections.Generic;
using System.Data;
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
                Dienste.Dialog.Meldung("Daten gespeichert");
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

        public string ProjektDelete(bool zuletzt = false)
        {
            ProjektCtrl ctrlproj = new ProjektCtrl();
            WErzeugerCtrl ctrlwerz = new WErzeugerCtrl();
            Form_ProjektDelete frm = new Form_ProjektDelete();
            string szProjekt = "";

            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK && frm.szProjekt != "")
            {
                // --- Sicherheitsabfrage vor dem tatsächlichen Löschen ---
                // warnend: das Warnsymbol ist hier eine Aussage.
                // vorgabeNein: der Fokus liegt zur Sicherheit auf "Nein", damit die
                // Eingabetaste kein Projekt löscht.
                bool loeschen = Dienste.Dialog.Frage(
                    $"Sind Sie sicher, dass Sie das Projekt '{frm.szProjekt}' und alle dazugehörigen Daten unwiderruflich löschen möchten?",
                    "Projekt löschen bestätigen",
                    warnend: true,
                    vorgabeNein: true);

                // Wenn der Nutzer nicht auf "Ja" klickt, wird der Löschvorgang abgebrochen
                if (!loeschen)
                {
                    return "";
                }

                try
                {
                    // 1. Unhandlichen OdbcDataAdapter durch sauberes DataRepository.GetDataTable (OLEDB) ersetzt
                    string selectSql = "SELECT * FROM Tab_Applikation";
                    DataTable dt = DataRepository.GetDataTable(selectSql);

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        if (row["ID_Projekt"] != DBNull.Value && Convert.ToInt32(row["ID_Projekt"]) == frm.ID_Projekt)
                        {
                            // 2. Statt speicherintensiven CommandBuilder upzudaten, führen wir ein gezieltes UPDATE per Repository aus
                            string updateSql = "UPDATE Tab_Applikation SET Projektname = ?, ID_Projekt = 0";
                            DbParam pName = new DbParam("?", "");

                            DataRepository.ExecuteNonQuery(updateSql, pName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Zurücksetzen der Tab_Applikation: " + ex.Message);
                    Dienste.Dialog.Fehler($"Fehler beim Zurücksetzen der Applikationsdaten: {ex.Message}", "Fehler");
                    return "";
                }

                ctrlwerz.ID_Projekt = frm.ID_Projekt;
                ctrlwerz.Delete();

                ctrlproj.m_szProjektname = frm.szProjekt;
                ctrlproj.Delete(frm.szProjekt);
                szProjekt = frm.szProjekt;

                // --- NEU: Erfolgsmeldung nach erfolgreichem Löschen ---
                Dienste.Dialog.Meldung($"Das Projekt '{szProjekt}' wurde erfolgreich gelöscht.", "Projekt gelöscht");
            }
            return szProjekt;
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