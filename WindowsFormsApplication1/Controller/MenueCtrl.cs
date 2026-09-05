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
    /// <c>Program.startfrm</c> — das Feld selbst ist mit iU9-W16b.3 entfallen.</para>
    ///
    /// <para>Das Feld <c>wizparent</c> ist mit iU5 entfallen: Es wurde ausschliesslich
    /// beschrieben und von niemandem gelesen; der Assistentenrahmen haengt seit Paket P4
    /// ueber <c>WizardParent.Aktiver</c> und <c>WizardCtrl.Aktueller</c>.</para>
    /// </summary>
    class MenueCtrl
    {
        /// <summary>
        /// Öffnet den Projektassistenten für ein NEUES Projekt.
        ///
        /// <para>
        /// <b>P4 (Projektdialoge vereinheitlichen): eine Seitenliste statt zwei.</b>
        /// Die dreizehn Zeilen Seitenaufbau standen hier und in
        /// <see cref="ProjektBearbeiten"/> wortgleich doppelt; sie liegen seit
        /// iU9-W16a.5 in <c>EPOS.UI/Seiten/Assistent/AssistentSeite.razor</c>.
        /// Reihenfolge und Inhalt sind unverändert — die beiden Einstiege
        /// unterscheiden sich nur noch in der Betriebsart.
        /// </para>
        /// </summary>
        public void ProjektNeu()
        {
            AssistentZeigen(AssistentCtrl.BETRIEBSART_NEU);
        }

        /// <summary>
        /// Öffnet den Projektassistenten für ein BESTEHENDES Projekt (linke Spalte =
        /// der Baustein <c>ProjektListe</c>). Seitenliste wie in
        /// <see cref="ProjektNeu"/>.
        /// </summary>
        public void ProjektBearbeiten()
        {
            AssistentZeigen(AssistentCtrl.BETRIEBSART_BEARBEITEN);
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
        /// Öffnet ein Projekt — es wird das AKTIVE Projekt der Startseite.
        ///
        /// <para>
        /// <b>P3 (Projektdialoge vereinheitlichen): „Öffnen" öffnet jetzt wirklich.</b>
        /// Bis dahin zeigte dieser Menüweg <c>Form_ProjektSpeichernUnter</c>,
        /// verlangte einen NEUEN Projektnamen und DUPLIZIERTE das Projekt; erst danach
        /// wurde das Ausgangsprojekt geöffnet. Duplizieren heißt jetzt ausschließlich
        /// „Speichern unter…"; hier steht die Projektauswahl (seit iU9-W15a die
        /// Razor-Komponente <c>ProjektWahlDialog</c>) mit Liste, Suche und Sortierung.
        /// </para>
        /// <para>
        /// <b>iU9-W16b.1 (Anwenderentscheid E-7, K6-a): kein Detailformular mehr.</b>
        /// Bis hierher lud der Weg das Projekt in <c>FormMain</c> — zwölf
        /// Gewerkslisten, elf Kontextmenüs, modal. Dieses Fenster ist mit dem
        /// Altzweig stillgelegt (3 811 Zeilen); an seine Stelle tritt die Startseite,
        /// die dieselben zwölf Gewerke als Kacheln führt. Beide Einstiege — „Öffnen…"
        /// und „Zuletzt geöffnet" — machen das gewählte Projekt deshalb nur noch
        /// AKTIV, genau wie die gleichnamigen Kacheln (<see cref="ProjektAktivSetzen"/>).
        /// </para>
        /// </summary>
        /// <param name="zuletzt">true = ohne Dialog das zuletzt geöffnete Projekt laden.</param>
        public bool ProjektOeffnen(bool zuletzt = false)
        {
            if (!zuletzt)
            {
                Projektwahl wahl = new Projektwahl();
                if (!Dienste.Navigation.OeffneMaske(Masken.ProjektAuswahl, wahl)) return false;
                if (wahl.Id <= 0 || wahl.Name == "") return false;

                return ProjektAktivSetzen(wahl.Name, wahl.Id);
            }

            ApplikationCtrl ctrl = new ApplikationCtrl();
            ctrl.ReadSingle();
            if (ctrl.m_szProjektname == "") return false;
            return ProjektAktivSetzen(ctrl.m_szProjektname, ctrl.m_ID_Projekt);
        }

        /// <summary>
        /// Macht ein Projekt zum AKTIVEN Projekt der Startmaske.
        ///
        /// <para>
        /// <b>Seit iU9-W16b.1 ist das der EINZIGE Öffnungsweg.</b> Bis dahin stand
        /// daneben <c>ProjektInFormMainLaden</c>: Es baute das Detailformular
        /// „Konfiguration Projekt", bestückte zwölf Listen und elf Kontextmenüs und
        /// zeigte es modal. Mit dem Anwenderentscheid E-7 (K6-a) ist dieses Fenster
        /// gelöscht; „Öffnen…", „Zuletzt geöffnet" und die gleichnamigen Kacheln gehen
        /// jetzt alle hier durch. Das Projekt wird das aktive: Startseite zeigt es,
        /// „zuletzt geöffnet" merkt es sich, kein Fenster geht auf (Nutzerwunsch
        /// 30.08.2026 zum Knopf „Projekt öffnen" im Assistenten).
        /// </para>
        /// <para>
        /// <b>Eine Wahrheit.</b> Alles, was die Startmaske nachziehen muss — Name/ID,
        /// Kopfband, Klimaregion, Statuszeichen, Freischaltung der Reiter, Kachelstatus
        /// (Bitmaske) und Variantenanzeige —, steht bereits in
        /// <see cref="ProjektKontextCtrl"/> (K2, seit iU9-W16b.0 im Kern); das Merken
        /// in <c>Tab_Applikation</c> ebenfalls dort.
        /// Beides wird hier nur AUFGERUFEN, nichts davon nachgebaut. Die Klimaregion
        /// liest dabei über <c>StartseiteCtrl.ProjektKlimazone</c> die PROJEKTKOPIE
        /// (<c>Tab_Klimaregion</c>) statt des Stammsatzes — seit dem
        /// Anwenderentscheid W16b‑O‑3 (04.09.2026) auf beiden Plattformen dieselbe
        /// Antwort; die abweichende Stammabfrage der iOS-Hülle ist gefallen.
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
        /// Der LOESCHWEG eines Projekts — Auswahl, Sicherheitsabfrage, die drei
        /// Loeschschritte, Erfolgsmeldung.
        ///
        /// <para><b>iU9-W15a.2:</b> Die Auswahl UND die Sicherheitsabfrage stehen jetzt in
        /// derselben Razor-Komponente (<c>ProjektWahlDialog</c> mit
        /// <c>ProjektZweck.Loeschen</c>) — der Anwender sieht die Frage dort, wo er
        /// gerade ist. Die REIHENFOLGE der sechs Schritte ist unveraendert; die Schritte
        /// 3 bis 5 (Tab_Applikation zuruecksetzen, Energieanlagen, Projekt) liegen seit
        /// iU9-W15a.0d als <see cref="ProjektCtrl.LoeschenMitVorarbeiten"/> im Kern.</para>
        ///
        /// <para><b>Ein mehrdeutiger Projektname</b> (Entscheid W15a-O-3 vom 04.09.2026):
        /// Der Loeschweg laeuft ueber den NAMEN. Traegt eine Datenbank zwei Projekte
        /// desselben Namens — regulaer unmoeglich, <c>Tab_Projekt</c> hat den eindeutigen
        /// Index <c>Projektname</c>, aber ein Altbestand ohne ihn kann es —, dann fragt
        /// der DIALOG nach und meldet die Zustimmung ueber
        /// <c>Projektwahl.AlleGleichenNamens</c>; ohne sie bricht der Kern ab und es wird
        /// nichts geloescht.</para>
        ///
        /// <para>Der Rueckgabewert bleibt der Projektname bei Erfolg und <c>""</c> sonst —
        /// beide Aufrufer werten genau das aus.</para>
        /// </summary>
        public string ProjektDelete(bool zuletzt = false)
        {
            Projektwahl wahl = new Projektwahl();

            // Der Dialog liefert nur mit OK zurueck, und OK gibt es im Loeschmodus erst
            // nach der Sicherheitsabfrage (warnend, Vorgabe "Nein" - damit die
            // Eingabetaste kein Projekt loescht).
            if (!Dienste.Navigation.OeffneMaske(Masken.ProjektDelete, wahl) || wahl.Name == "")
                return "";

            // Sicherungskopie und Mehrfachauswahl (Nutzerauftrag 02.09.2026; mit Merge 5 aus
            // Form_ProjektDelete in den ProjektWahlDialog portiert). Der Dialog hat bereits
            // mit der vollstaendigen Liste zurueckgefragt.
            if (wahl.SicherungGewuenscht && !DatenbankSichern("vor_Loeschen")) return "";
            if (wahl.Mehrere.Count > 1) return MehrereLoeschen(wahl.Mehrere);

            LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(
                wahl.Id, wahl.Name, wahl.AlleGleichenNamens);

            // Der Kern hat abgebrochen, ohne etwas anzufassen: Der Name trifft mehrere
            // Projekte, und die Zustimmung fehlt. Hier ist das das SICHERUNGSNETZ - die
            // Rueckfrage steht im Dialog; ein "Nein" kommt gar nicht bis hierher.
            if (befund.Stand == LoeschStand.Mehrdeutig)
            {
                if (!Dienste.Dialog.Frage(
                        string.Format(Text_("PROJ_MSG_NAME_MEHRDEUTIG",
                            "Der Projektname „{0}“ ist {1}-mal vergeben. Alle {1} Projekte werden "
                            + "gelöscht. Fortfahren?"), befund.Projektname, befund.Anzahl),
                        Text_("PROJ_MSG_NAME_MEHRDEUTIG_TITEL", "Projektname mehrfach vergeben"),
                        warnend: true, vorgabeNein: true))
                    return "";

                befund = ProjektCtrl.LoeschenMitVorarbeiten(wahl.Id, wahl.Name,
                                                           mehrdeutigZugelassen: true);
            }

            if (befund.Stand == LoeschStand.ApplikationsdatenFehler)
            {
                Dienste.Dialog.Fehler(
                    string.Format(Text_("PRJ_DEL_MSG_APPFEHLER",
                        "Fehler beim Zurücksetzen der Applikationsdaten: {0}"), befund.Fehlertext),
                    Text_("SIM_TITEL_FEHLER", "Fehler"));
                return "";
            }

            if (befund.Stand != LoeschStand.Geloescht) return "";

            Dienste.Dialog.Meldung(
                string.Format(Text_("PRJ_DEL_MSG_ERFOLG",
                    "Das Projekt '{0}' wurde erfolgreich gelöscht."), befund.Projektname),
                Text_("PRJ_DEL_MSG_ERFOLG_TITEL", "Projekt gelöscht"));

            return befund.Projektname;
        }

        /// <summary>
        /// Loescht mehrere Projekte hintereinander - Varianten VOR ihren Staemmen, wie der
        /// Dialog sie liefert. Je Projekt der Kernweg (Anlagen, Ergebnisse, Projektzeile samt
        /// Kaskaden). Rueckgabe wie beim Einzelweg ein Name: der des aktiven Projekts, falls
        /// es dabei war (Form_Start setzt dann seinen Platzhalter), sonst der zuletzt
        /// geloeschte; leer, wenn nichts geschah.
        /// </summary>
        private string MehrereLoeschen(List<ProjektKopfZeile> liste)
        {
            int aktuell = AktuelleProjektId();
            string aktuellerName = "", letzter = "";
            int n = 0;
            var fehler = new List<string>();
            foreach (ProjektKopfZeile p in liste)
            {
                LoeschBefund befund = ProjektCtrl.LoeschenMitVorarbeiten(p.Id, p.Name, mehrdeutigZugelassen: false);
                if (befund.Stand != LoeschStand.Geloescht)
                {
                    fehler.Add(p.Name + (string.IsNullOrEmpty(befund.Fehlertext) ? "" : " (" + befund.Fehlertext + ")"));
                    continue;
                }
                n++;
                letzter = p.Name;
                if (p.Id == aktuell) aktuellerName = p.Name;
            }

            string meldung = string.Format(Text_("PDLG_ERFOLG", "{0} Projekt(e) gelöscht."), n);
            if (fehler.Count > 0)
                meldung += Environment.NewLine + Text_("PDLG_FEHLER", "Fehler bei:") + " " + string.Join(", ", fehler);
            string titel = Text_("PDLG_TITEL", "Projekte löschen");
            if (fehler.Count > 0) Dienste.Dialog.Warnung(meldung, titel);
            else Dienste.Dialog.Meldung(meldung, titel);

            return aktuellerName.Length > 0 ? aktuellerName : letzter;
        }

        // ------------------------------------------------------------------
        // Sicherungshelfer des Löschwegs mit Mehrfachauswahl (Nutzerauftrag 02.09.2026,
        // WinForms-Fassung Form_ProjektDelete). Die Maske ist mit iU9‑W15a.2 durch
        // ProjektWahlDialog ersetzt; die Helfer bleiben für die Portierung der
        // Mehrfachlöschung samt Sicherungskopie (Merge 5, 05.09.2026).
        // ------------------------------------------------------------------
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
                    string.Format(Text_("PDLG_SICHERUNG_FEHLER",
                        "Die Sicherungskopie konnte nicht angelegt werden:\r\n{0}\r\n\r\nTrotzdem löschen?"), ex.Message),
                    Text_("PDLG_TITEL", "Projekte löschen"),
                    warnend: true,
                    vorgabeNein: true);
            }        }

        /// <summary>Anzeigetext aus dem Ressourcenkatalog; Rueckfall = der deutsche Satz.</summary>
        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        // ==================================================================
        //  iU9-W16c.3: DIE EINUNDZWANZIG EINZEILER SIND WEG
        //
        //  Bis hierher standen hier 21 Methoden der Bauform
        //      public void X() { Dienste.Navigation.OeffneMaske(Masken.X); }
        //  - WP_Administration, StromspeicherBearbeiten, PeakShavingBearbeiten,
        //  GebaeudeBearbeiten, GebaeudetypenBearbeiten, WaermebedarfExtern,
        //  Prozesswaerme, Stromverbraucher, Stromganglinie, Solarganglinie,
        //  WPImport, Kessel, BHKW, Solarkollektoren, PV, SPKImport,
        //  PufferSPImport, PufferSp, Brauchwasser, PVImport,
        //  SolarThermieImport.
        //
        //  IHR EINZIGER AUFRUFER WAR DAS MENUE DES HAUPTFENSTERS - je einer der
        //  34 Ereignishandler von Hauptfensterrahmen. Seit W16c.1 steht der
        //  Maskenschluessel in der Menuetabelle selbst, und
        //  HauptfensterHuelle.Weg reicht ihn unmittelbar an
        //  Dienste.Navigation.OeffneMaske weiter. Eine Methode, die nichts tut,
        //  als einen Schluessel weiterzugeben, waere danach eine Zwischenstufe
        //  ohne Aufgabe.
        //
        //  WAS BLEIBT, sind die ZUSAMMENGESETZTEN Ablaeufe: der Assistent mit
        //  seinen zwei Betriebsarten, das Oeffnen samt Aktivsetzen, das
        //  Duplizieren und der sechsschrittige Loeschweg. Sie haben Aufrufer in
        //  der Startseiten- und der Assistentenhuelle - und sie tun mehr als
        //  einen Schluessel zu nennen.
        // ==================================================================
    }
}
