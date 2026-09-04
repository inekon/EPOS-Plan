using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Menuewege der Anwendung: Projekt anlegen, oeffnen, duplizieren, loeschen
    /// und die Stammdaten- und Einlesemasken.
    ///
    /// <para><b>Seit iU5 kennt dieser Controller keine Maske mehr.</b> Er nennt einen
    /// sprachneutralen Maskenschluessel (<see cref="Masken"/>) und ueberlaesst das
    /// Bauen und Zeigen <c>Dienste.Navigation</c>; das offene Projekt fuehrt
    /// <c>Dienste.Projekt</c>. Vorher standen hier 25 Aufrufe der Bauform
    /// <c>new Form_X(); frm.ShowDialog();</c> und neun Zugriffe auf
    /// <c>Program.mainfrm</c>/<c>Program.startfrm</c>.</para>
    ///
    /// <para>Das Feld <c>wizparent</c> ist mit iU5 entfallen: Es wurde ausschliesslich
    /// beschrieben und von niemandem gelesen; der Assistentenrahmen haengt seit Paket P4
    /// ueber <c>WizardParent.Aktiver</c> und <c>WizardCtrl.Aktueller</c>.</para>
    /// </summary>
    class MenueCtrl
    {
        /// <summary>
        /// Zieht den Projektnamen aus <c>Tab_Applikation</c> in den Kopf des
        /// Detailformulars nach.
        /// </summary>
        public void SetProjektname()
        {
            Dienste.Navigation.AnsichtAktualisieren(Ansichten.ProjektDetail);
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
            // Der Rahmen wird von der Navigation gebaut, beim WizardCtrl angemeldet und
            // modal gezeigt; hier bleibt nur die Rueckmeldung an den Anwender.
            if (Dienste.Navigation.OeffneMaske(Masken.Assistent, betriebsart))
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
                Projektwahl wahl = new Projektwahl();
                if (!Dienste.Navigation.OeffneMaske(Masken.ProjektAuswahl, wahl)) return;
                if (wahl.Id <= 0 || wahl.Name == "") return;

                ProjektInFormMainLaden(wahl.Name, wahl.Id);
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
            // Der Ablauf selbst - Stammdaten lesen, zwoelf Gewerkslisten und elf
            // Kontextmenues bestuecken, modal zeigen, danach den Projektkontext der
            // Startmaske nachziehen - ist von der ersten bis zur letzten Anweisung
            // Oberflaechenarbeit und steht seit iU5 zeilengleich in WinFormsNavigation.
            Dienste.Navigation.OeffneMaske(Masken.ProjektDetail, szProjekt, idProjekt);
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
            return Dienste.Projekt.Uebernehmen(idProjekt, szProjekt);
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
            return Dienste.Navigation.OeffneMaske(Masken.ProjektSpeichernUnter);
        }

        public string ProjektDelete(bool zuletzt = false)
        {
            ProjektCtrl ctrlproj = new ProjektCtrl();
            WErzeugerCtrl ctrlwerz = new WErzeugerCtrl();
            Projektwahl wahl = new Projektwahl();
            string szProjekt = "";

            if (Dienste.Navigation.OeffneMaske(Masken.ProjektDelete, wahl) && wahl.Name != "")
            {
                // --- Sicherheitsabfrage vor dem tatsächlichen Löschen ---
                // warnend: das Warnsymbol ist hier eine Aussage.
                // vorgabeNein: der Fokus liegt zur Sicherheit auf "Nein", damit die
                // Eingabetaste kein Projekt löscht.
                bool loeschen = Dienste.Dialog.Frage(
                    $"Sind Sie sicher, dass Sie das Projekt '{wahl.Name}' und alle dazugehörigen Daten unwiderruflich löschen möchten?",
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
                        if (row["ID_Projekt"] != DBNull.Value && Convert.ToInt32(row["ID_Projekt"]) == wahl.Id)
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

                ctrlwerz.ID_Projekt = wahl.Id;
                ctrlwerz.Delete();

                ctrlproj.m_szProjektname = wahl.Name;
                ctrlproj.Delete(wahl.Name);
                szProjekt = wahl.Name;

                // --- NEU: Erfolgsmeldung nach erfolgreichem Löschen ---
                Dienste.Dialog.Meldung($"Das Projekt '{szProjekt}' wurde erfolgreich gelöscht.", "Projekt gelöscht");
            }
            return szProjekt;
        }

        public void WP_Administration()
        {
            Dienste.Navigation.OeffneMaske(Masken.WpAdministration);
        }

        public void StromspeicherBearbeiten()
        {
            Dienste.Navigation.OeffneMaske(Masken.StromspeicherAdmin);
        }

        /// <summary>
        /// Öffnet die Lastspitzenkappung (Peak-Shaving) – eigener Einstieg nach
        /// Fachkonzept 6.4 (AP7). Ein geöffnetes Projekt ist ausdrücklich nicht
        /// nötig: ohne Projekt stehen Stammganglinien und der Direktimport zur
        /// Verfügung, deshalb hier auch keine Projektprüfung.
        /// </summary>
        public void PeakShavingBearbeiten()
        {
            Dienste.Navigation.OeffneMaske(Masken.PeakShaving, Dienste.Projekt.Id);
        }

        public void GebaeudeBearbeiten()
        {
            Dienste.Navigation.OeffneMaske(Masken.GebaeudeAdmin);
        }

        public void GebaeudetypenBearbeiten()
        {
            Dienste.Navigation.OeffneMaske(Masken.GebaeudetypenAdmin);
        }

        public void WaermebedarfExtern()
        {
            Dienste.Navigation.OeffneMaske(Masken.WaermebedarfExternAdmin);
        }

        public void Prozesswaerme()
        {
            Dienste.Navigation.OeffneMaske(Masken.ProzesswaermeAdmin);
        }

        public void Stromverbraucher()
        {
            Dienste.Navigation.OeffneMaske(Masken.StromverbraucherAdmin);
        }

        public void Stromganglinie()
        {
            Dienste.Navigation.OeffneMaske(Masken.StromganglinieAdmin);
        }

        public void Solarganglinie()
        {
            Dienste.Navigation.OeffneMaske(Masken.SolarganglinieAdmin);
        }

        public void WPImport()
        {
            Dienste.Navigation.OeffneMaske(Masken.WpImport);
        }

        public void Kessel()
        {
            Dienste.Navigation.OeffneMaske(Masken.HeizkesselAdmin);
        }

        public void BHKW()
        {
            Dienste.Navigation.OeffneMaske(Masken.BhkwAdmin);
        }
        public void Solarkollektoren()
        {
            Dienste.Navigation.OeffneMaske(Masken.SolarkollektorenAdmin);
        }

        public void PV()
        {
            Dienste.Navigation.OeffneMaske(Masken.PvAdmin);
        }

        public void SPKImport()
        {
            Dienste.Navigation.OeffneMaske(Masken.HeizkesselImport);
        }

        public void PufferSPImport()
        {
            Dienste.Navigation.OeffneMaske(Masken.PufferSpImport);
        }

        public void PufferSp()
        {
            Dienste.Navigation.OeffneMaske(Masken.PufferSpAdmin);
        }

        public void Brauchwasser()
        {
            Dienste.Navigation.OeffneMaske(Masken.BrauchwasserAdmin);
        }

        /// <summary>
        /// Herstellerdaten PV-Module einlesen — die CEC-Modulliste.
        ///
        /// <para><b>Sie war LEER</b> (Befund W13-B52): Der Menuepunkt
        /// <c>MenuItem_PV_Import</c> rief sie, und es geschah nichts. Seit
        /// iU9-W13.0k oeffnet sie die Maske ueber ihren Schluessel — wie jeder
        /// andere Menueweg auch.</para>
        /// </summary>
        public void PVImport()
        {
            Dienste.Navigation.OeffneMaske(Masken.PvImport, "CEC");
        }

        public void SolarThermieImport()
        {
            Dienste.Navigation.OeffneMaske(Masken.SolarkollektorenImport);
        }
    }
}