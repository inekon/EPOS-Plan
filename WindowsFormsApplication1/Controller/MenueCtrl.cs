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
            // Der Dialog liefert die Häkchenauswahl über sein Fach (iU5: der Controller
            // kennt die Maske nicht) und hat mit der vollständigen Liste bereits
            // zurückgefragt — die Einzelrückfrage des Bestands ist damit abgelöst.
            Projektloeschwahl wahl = new Projektloeschwahl();
            if (!Dienste.Navigation.OeffneMaske(Masken.ProjektDelete, wahl)) return "";

            List<ProjektModel> liste = wahl.ZuLoeschen;
            if (liste == null || liste.Count == 0) return "";

            if (wahl.SicherungGewuenscht && !DatenbankSichern("vor_Loeschen")) return "";

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
            string titel = Form_ProjektDelete.TPd("PDLG_TITEL", "Projekte löschen");
            if (fehler.Count > 0) Dienste.Dialog.Warnung(meldung, titel);
            else Dienste.Dialog.Meldung(meldung, titel);

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
            DbParam pName = new DbParam("?", "");
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
                // warnend + vorgabeNein: dieselbe Aussage wie zuvor über
                // MessageBoxIcon.Warning und MessageBoxDefaultButton.Button2 — ohne
                // Sicherung soll die Eingabetaste nicht löschen.
                return Dienste.Dialog.Frage(
                    string.Format(Form_ProjektDelete.TPd("PDLG_SICHERUNG_FEHLER",
                        "Die Sicherungskopie konnte nicht angelegt werden:\r\n{0}\r\n\r\nTrotzdem löschen?"), ex.Message),
                    Form_ProjektDelete.TPd("PDLG_TITEL", "Projekte löschen"),
                    warnend: true,
                    vorgabeNein: true);
            }
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

        public void PVImport()
        {

        }

        public void SolarThermieImport()
        {
            Dienste.Navigation.OeffneMaske(Masken.SolarkollektorenImport);
        }
    }
}