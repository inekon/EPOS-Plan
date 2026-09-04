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
        /// Bis dahin zeigte dieser Menüweg <c>Form_ProjektSpeichernUnter</c>,
        /// verlangte einen NEUEN Projektnamen und DUPLIZIERTE das Projekt; erst danach
        /// wurde das Ausgangsprojekt geöffnet. Duplizieren heißt jetzt ausschließlich
        /// „Speichern unter…"; hier steht die Projektauswahl (seit iU9-W15a die
        /// Razor-Komponente <c>ProjektWahlDialog</c>) mit Liste, Suche und Sortierung.
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

        /// <summary>Anzeigetext aus dem Ressourcenkatalog; Rueckfall = der deutsche Satz.</summary>
        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
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