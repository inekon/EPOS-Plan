using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE QUELLE der Ansicht ASSISTENT (Befund <b>W16a-O-4</b>) — der
    /// Kern dessen, was bis hierher <c>Views/Wizard/AssistentHuelle</c> allein tat.
    ///
    /// <para><b>Warum sie umgezogen ist.</b> <c>AssistentHuelle</c> baut die
    /// Datenseite EINES Assistentenlaufs. Davon war genau zweierlei Windows: die elf
    /// Seitenhüllen mit ihrem Fensterbesitzer und die zwei Kurzhinweise der
    /// Startseite. Alles Übrige — Betriebsart, Projektliste, die drei Ablaufdelegaten,
    /// der Speicherlauf, der Nachzug des Projektkontexts, die achtzehn Texte — ruft
    /// ausschließlich Kern-Controller und <c>Dienste.*</c>. Solange das Ganze in der
    /// Windows-Anwendung lag, meldete <c>AppWurzel</c> auf dem iPad, der Assistent
    /// stehe dort nicht zur Verfügung. Dieselbe Begründung und dasselbe Muster wie bei
    /// <see cref="SimulationAnsichtQuelle"/> (Auftrag #208).</para>
    ///
    /// <para><b>Es wird nichts nachgebaut.</b> Windows ruft seit dem Umzug DIESE
    /// Klasse; <c>AssistentHuelle</c> ist nur noch die Schale, die die Naht
    /// (<see cref="AssistentPlattformwege"/>) füllt. Zwei Fassungen desselben
    /// Assistentenlaufs wären die Stelle, an der iPad und Windows verschiedene
    /// Projekte schreiben.</para>
    ///
    /// <para><b>Der Speicherweg bleibt unangetastet</b> (W16a-O-1): <c>Speichern</c>
    /// ruft <see cref="AssistentCtrl.Speichern"/>, und der klammert seit Aufgabe #62a
    /// den ganzen Lauf in EINEN <c>DbVorgang</c>. Hier steht davon nur der Aufruf.</para>
    /// </summary>
    internal static class AssistentAnsichtQuelle
    {
        /// <summary>
        /// Der PARAMETERSATZ EINES neuen Assistentenlaufs — er legt den
        /// <see cref="AssistentCtrl"/> des Laufs an und reicht ihn über die Delegaten
        /// in <see cref="Gaben"/> an die Razor-Seite. Gerufen wird er, sobald die
        /// Ansicht ASSISTENT betreten wird — einmal je Lauf.
        /// </summary>
        /// <param name="betriebsart">
        /// <see cref="AssistentCtrl.BETRIEBSART_NEU"/> oder <c>…_BEARBEITEN</c>.
        /// </param>
        /// <param name="wege">Die Naht zur Schale; <c>null</c> = keiner der Wege.</param>
        /// <param name="vorauswahlId">
        /// Vorauswahl des linken Bandes im Bearbeiten-Zweig; <c>0</c> = keine. Unter
        /// Windows bleibt sie <c>0</c> — dort wählt der Anwender im Band selbst. Der
        /// iOS-Einstieg kommt dagegen aus der Zeile EINES Projekts und bringt es mit.
        /// </param>
        internal static IReadOnlyDictionary<string, object> AnsichtGaben(
            int betriebsart, AssistentPlattformwege wege, int vorauswahlId = 0)
        {
            AssistentCtrl ctrl = new AssistentCtrl { Betriebsart = betriebsart };

            // Ob dieser Lauf das Projekt als „zuletzt geoeffnet" merkt, entscheidet
            // der EINSTIEG und nicht der Speicherweg: Die Startkacheln tun es, die
            // zwei Menuewege „Neu"/„Bearbeiten" nicht (ProjektKontextCtrl, Setzen
            // gegen Uebernehmen). In der modalen Fassung stand diese Unterscheidung
            // in den zwei Aufrufern, die nach dem ShowDialog weiterarbeiteten; jetzt
            // meldet der Einstieg sie vorher an - einmalig, fuer genau diesen Lauf.
            bool merken = _merkeNaechstenLauf;
            _merkeNaechstenLauf = false;

            return new Dictionary<string, object>(Gaben(ctrl, wege, merken, vorauswahlId));
        }

        /// <summary>
        /// Der nächste Lauf merkt sein Projekt als „zuletzt geöffnet" — gesetzt von
        /// den zwei Startkacheln, verbraucht von <see cref="AnsichtGaben"/>.
        /// </summary>
        internal static void NaechsterLaufMerktProjekt()
        {
            _merkeNaechstenLauf = true;
        }

        private static bool _merkeNaechstenLauf;

        /// <summary>
        /// Der PARAMETERSATZ der Seite — die Delegaten der Vermessung § 12.8 (Laden,
        /// Speichern, Seite schalten) samt den Texten.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            AssistentCtrl ctrl, AssistentPlattformwege wege,
            bool merken = false, int vorauswahlId = 0)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));
            AssistentPlattformwege w = wege ?? new AssistentPlattformwege();

            // Der Speicherlauf haengt an WizardCtrl.Aktueller (AssistentCtrl.Speichern
            // :723). Unter Windows legt Program.Main den Halter an; eine Schale ohne
            // Program - die iOS-Huelle - haette hier keinen, und der erste Speicherlauf
            // scheiterte mit „WizardCtrl". Die Zeile legt ihn an, wenn er fehlt, und
            // laesst einen vorhandenen in Ruhe.
            if (WizardCtrl.Aktueller == null) WizardCtrl.Aktueller = new WizardCtrl();

            // Die geteilten Listen der dreizehn Seiten. Sie leben so lange wie der
            // Lauf; die Seiten bearbeiten sie an Ort und Stelle.
            List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile> komponenten =
                new List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile>();

            string[] gewaehlterName = { "" };

            return new Dictionary<string, object>
            {
                ["Betriebsart"] = ctrl.Betriebsart,
                ["Projekte"] = Projektliste(),

                // W16a-O-4: Die Vorauswahl des linken Bandes. Unter Windows 0 - dort
                // markiert der Anwender selbst; der iOS-Einstieg kommt aus der Zeile
                // EINES Projekts und braeuchte sonst eine zweite Auswahl derselben
                // Sache.
                ["VorauswahlId"] = vorauswahlId,

                ["SeiteAktiv"] = new Func<int, bool>(ctrl.SeiteAktiv),

                ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>>(
                    nr => Seitengaben(ctrl, nr, w, komponenten, gewaehlterName[0])),

                ["SeiteVerlassen"] = new Action<int>(nr => SeiteVerlassen(ctrl, nr)),
                ["SeitePruefen"] = new Func<int, string>(nr => SeitePruefen(ctrl, nr)),

                // W16a-O-4: Ein Schritt, den DIESE Schale nicht bedient, wird BENANNT
                // abgelehnt und faellt nicht still als leere Seite aus (Hausregel der
                // Simulationsnaht). Unter Windows ist der Grund leer - dort gibt es
                // keinen solchen Schritt.
                ["SeiteSperrgrundText"] = w.SeitenSperrgrund ?? "",

                ["ProjektMarkiert"] = new Action<int, string>((id, name) =>
                {
                    // Woertlich ucProjektAuswahl_MarkierungGeaendert: Bestand neu
                    // lesen, Seiten danach stellen, Ladekennzeichen zuruecksetzen.
                    ctrl.ProjektId = id;
                    gewaehlterName[0] = name ?? "";
                    KomponentenauswahlHuelle.Gaben(id, komponenten, ctrl.Betriebsart,
                                                   ctrl.SeiteSchalten);
                    ctrl.BereitsGeladen = false;

                    // 62b-E-1: Ein anderes Projekt ist ein anderer Ausgangsstand -
                    // die dreizehn Seitenschalter kommen aus SEINEM Komponenten-
                    // bestand, nicht aus einer Eingabe. Ohne diese Zeile meldete
                    // der Assistent nach einem blossen Listenklick „ungespeichert".
                    ctrl.ZustandMerken();
                }),

                ["ProjektOeffnen"] = new Action<int, string>((id, name) => ProjektOeffnen(id, name, w)),

                ["Speichern"] = new Func<(string Text, string Titel)?>(() =>
                {
                    AssistentErgebnis e = ctrl.Speichern();
                    if (!e.Erfolg)
                        return (AssistentCtrl.Meldungstext(e), AssistentCtrl.Meldungstitel(e));

                    // Der Speicherweg selbst bleibt unangetastet (W16a-O-1). Was
                    // hier DAHINTER steht, ist der Nachzug, den bis #62b die zwei
                    // Aufrufer nach dem ShowDialog erledigten.
                    ctrl.ZustandMerken();
                    ProjektkontextNachziehen(merken);

                    // Die Meldung „Daten gespeichert" stand bis #62b als MessageBox
                    // in MenueCtrl.AssistentZeigen, hinter dem ShowDialog. Sie steht
                    // jetzt hier - an der einen Stelle, die weiss, dass geschrieben
                    // wurde - und als Kurzhinweis der Startseite statt als Kasten
                    // (aus einem Blazor-Ereignis heraus ist eine MessageBox
                    // verboten, Regel (d) der Blazor-Huelle). Eine Schale ohne
                    // Startseite reicht keinen Haken herein und zeigt keinen.
                    w.Kurzhinweis?.Invoke(Text_("WIZ_GESPEICHERT", "Daten gespeichert"));
                    return null;
                }),

                // 62b-E-1: Woran der Assistent erkennt, dass es etwas zu verlieren
                // gibt. Die Antwort rechnet der Kern aus seinem Zustand. Der Entscheid
                // gilt auf BEIDEN Plattformen - ohne diesen Delegaten verloere ein
                // Lauf seine Eingaben beim Verlassen schweigend.
                ["HatAenderungen"] = new Func<bool>(() => ctrl.HatAenderungen),

                // Der fruehere FENSTERTITEL - er steht jetzt als Ueberschrift in der
                // Ansicht (#62b); der Ressourcenschluessel ist derselbe.
                ["TitelText"] = Text_("WIZ_TITEL", "Projektassistent"),

                ["AbbrechenText"] = Text_("WIZ_BTN_ABBRECHEN", "Abbrechen"),
                ["ZurueckText"] = Text_("WIZ_BTN_ZURUECK", "◀ Zurück"),
                ["WeiterText"] = Text_("WIZ_BTN_WEITER", "Weiter ▶"),
                ["SpeichernText"] = Text_("WIZ_BTN_SPEICHERN", "Speichern"),
                ["ProjektLabelText"] = Text_("WIZ_LBL_PROJEKT", "Bestehendes Projekt auswählen"),
                ["ProjektOeffnenText"] = Text_("WIZ_BTN_PROJEKT_OEFFNEN", "Projekt öffnen"),

                // Die drei Wege der Rueckfrage aus 62b-E-1; "Speichern" nimmt den
                // vorhandenen Knopftext - es IST derselbe Weg.
                ["VerlassenTitelText"] = Text_("WIZ_VERLASSEN_TITEL", "Ungespeicherte Eingaben"),
                ["VerlassenFrageText"] = Text_("WIZ_VERLASSEN_FRAGE",
                    "Der Projektassistent enthält Eingaben, die noch nicht gespeichert sind. "
                    + "Sollen sie jetzt gespeichert werden?"),
                ["VerwerfenText"] = Text_("WIZ_BTN_VERWERFEN", "Verwerfen"),
                ["BleibenText"] = Text_("WIZ_BTN_BLEIBEN", "Bleiben"),

                // W15a-E-1: Das linke Band zeigt nur den Namen; die Variantenherkunft
                // steht dort als leise Zeile darunter (keine Artspalte, kein Platz).
                ["ArtVarianteText"] = Text_("PRJ_LIST_ART_VARIANTE", "Variante"),
                ["VarianteVonFormat"] = Text_("PRJ_LIST_VARIANTE_VON", "Variante von {0}")
            };
        }

        // =================================================================================
        // Der Ablauf
        // =================================================================================

        /// <summary>
        /// Nutzerauftrag 02.09.2026 (mit Merge 5 aus <c>WizardParent.Next</c> und
        /// <c>Wizard_Projekt.Pruefe</c>): Pflichtfelder und Namensdoppel der
        /// Projektseite beim VERLASSEN prüfen — vorher fiel es erst beim Speichern
        /// auf, viele Seiten später. Liefert den Meldungstext oder <c>null</c>.
        /// </summary>
        private static string SeitePruefen(AssistentCtrl ctrl, int nr)
        {
            if (nr != WizardItemClass.PROJEKT_ITEM || ctrl.Kopf.Count == 0) return null;
            switch (ProjektKopfRegeln.Pruefe(ctrl.Kopf[0], ProjektKopfHuelle.VergebeneNamen()))
            {
                case ProjektKopfBefund.NameLeer:
                    return Text_("WZP_NAME_LEER", "Bitte einen Projektnamen eingeben.");
                case ProjektKopfBefund.NameVorhanden:
                    return Text_("WZP_NAME_VORHANDEN", "Ein Projekt mit diesem Namen existiert bereits.");
                case ProjektKopfBefund.KlimaLeer:
                    return Text_("WZP_KLIMA_LEER", "Bitte eine Klimaregion wählen.");
                default:
                    return null;
            }
        }

        /// <summary>
        /// Ein Schritt wird verlassen — wörtlich <c>WizardParent.Next</c> (:275-283):
        /// Beim Verlassen des Projektkopfes wandern seine sieben Felder in den
        /// Projektsatz, und beim ERSTEN Durchgang laufen die sechs Ladewege.
        /// </summary>
        private static void SeiteVerlassen(AssistentCtrl ctrl, int nr)
        {
            if (nr != WizardItemClass.PROJEKT_ITEM) return;

            ctrl.ProjektkopfUebernehmen();
            if (!ctrl.BereitsGeladen) ctrl.Laden(ctrl.Projekt.m_szProjektname);
        }

        /// <summary>
        /// Der Parametersatz EINER Seite. Die Zuordnung Nummer → Hülle ist die
        /// bitgleiche Übernahme von <c>AssistentSeiten.ERZEUGER</c>.
        ///
        /// <para><b>Zwei Seiten stehen hier, elf in der Naht.</b> Die
        /// Komponentenauswahl und der Projektkopf kennen keine Plattform — ihre Hüllen
        /// sind mit W16a-O-4 hierher gezogen. Die übrigen elf reichen bis heute einen
        /// Fensterbesitzer an die Katalogdialoge weiter, die sie aus sich heraus
        /// öffnen, und liegen deshalb in <c>WindowsFormsApplication1/Views</c>; sie
        /// kommen über <see cref="AssistentPlattformwege.SeitenGaben"/> herein und
        /// wandern mit iU11.</para>
        /// </summary>
        private static IReadOnlyDictionary<string, object> Seitengaben(
            AssistentCtrl ctrl, int nr, AssistentPlattformwege wege,
            List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile> komponenten,
            string projektName)
        {
            // Woertlich WizardParent.Next (:322-325): Im Neu-Zweig bekommt der Lauf
            // vor JEDEM Seitenaufbau eine geratene Id, an der die Auswahl-Dialoge
            // ihre noch ungespeicherten Zeilen aufhaengen.
            if (ctrl.Betriebsart == AssistentCtrl.BETRIEBSART_NEU)
                ctrl.ProjektId = new ProjektCtrl().GetMaxID() + 1;

            int id = ctrl.ProjektId;
            string name = ctrl.Betriebsart == AssistentCtrl.BETRIEBSART_BEARBEITEN
                ? projektName ?? "" : "";

            switch (nr)
            {
                case WizardItemClass.KOMPONENTEN_ITEM:
                    return KomponentenauswahlHuelle.Gaben(id, komponenten, ctrl.Betriebsart,
                                                          ctrl.SeiteSchalten);

                case WizardItemClass.PROJEKT_ITEM:
                    // NameAenderbar wird VOR dem Bestuecken gesetzt - der Ersatz fuer
                    // SetEditProjektName(bool): Bearbeiten heisst "Name steht fest".
                    ctrl.Kopf[0].NameAenderbar =
                        ctrl.Betriebsart == AssistentCtrl.BETRIEBSART_NEU;

                    // 62b-E-1: Was die Huelle hier eintraegt, ist der Stand der
                    // DATENBANK (Bearbeiten) bzw. die Vorbelegung eines neuen
                    // Projekts - beides keine Eingabe des Anwenders. Ob sie etwas
                    // eingetragen hat, sagt der Abdruck vorher/nachher; ohne diesen
                    // Vergleich verschluckte ein zweites Betreten der Projektseite
                    // eine Eingabe, die der Anwender dort gemacht hat.
                    string abdruckVorher = ctrl.KopfAbdruck();
                    IReadOnlyDictionary<string, object> kopfgaben =
                        ProjektKopfHuelle.Gaben(name, ctrl.Kopf);
                    if (ctrl.KopfAbdruck() != abdruckVorher) ctrl.KopfMerken();
                    return kopfgaben;

                default:
                    return wege.SeitenGaben == null ? null : wege.SeitenGaben(ctrl, nr, name);
            }
        }

        // =================================================================================
        // Der Rueckweg "Projekt oeffnen"
        // =================================================================================

        /// <summary>
        /// Setzt das gewählte Projekt aktiv — wörtlich
        /// <c>WizardParent.ProjektOeffnenUndSchliessen</c> (:940-960), ohne das
        /// Schließen: Das meldet seit #62b die Seite selbst
        /// (<c>Geschlossen(false)</c>), es gibt kein Fenster mehr.
        ///
        /// <para><b>Kein Detailformular</b> (Nutzerwunsch 30.08.2026): Der Anwender
        /// wollte an dieser Stelle nur wechseln, nicht bearbeiten.</para>
        ///
        /// <para><c>MenueCtrl.ProjektAktivSetzen</c> ist seit iU9-W16b.1 nichts
        /// anderes als <c>Dienste.Projekt.Uebernehmen</c> (MenueCtrl :135-138) — der
        /// Weg steht deshalb hier und braucht keine Naht.</para>
        /// </summary>
        private static void ProjektOeffnen(int id, string name, AssistentPlattformwege wege)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name)) return;

            if (!Dienste.Projekt.Uebernehmen(id, name)) return;

            // WizardCtrl.Aktueller.Projektname haelt den zuletzt GESPEICHERTEN Namen
            // und wird beim Start nicht geleert. Die Zeile stand hier, weil der
            // Nachzug nach dem ShowDialog aus diesem Feld las; sie bleibt, weil
            // ProjektkontextNachziehen denselben Weg geht.
            if (WizardCtrl.Aktueller != null) WizardCtrl.Aktueller.Projektname = name;

            // Der Kurzhinweis „Projekt X geoeffnet!" ueber der Startseite. In der
            // modalen Fassung wurde er nach dem ShowDialog gezeigt, weil Close()
            // den Rahmen nur ausblendete; jetzt wechselt die Ansicht unmittelbar
            // danach, und die Startseite holt den Satz beim naechsten Auffrischen ab.
            wege.HinweisProjektGeoeffnet?.Invoke();
        }

        /// <summary>
        /// Der NACHZUG des Projektkontexts nach einem gelungenen Speicherlauf
        /// (Aufgabe #62b).
        ///
        /// <para><paramref name="merken"/> trägt den EINEN Unterschied der zwei
        /// Einstiege: Die Startkacheln schreiben das Projekt als „zuletzt geöffnet"
        /// fort (<c>Uebernehmen</c>), die zwei Menüwege nicht (<c>Setzen</c>) —
        /// nachzulesen im Klassenkopf von <see cref="ProjektKontextCtrl"/>.</para>
        ///
        /// <para><b>Warum die Fallunterscheidung auf den Typ — Anwenderentscheid
        /// W16a-O-4-Q1 vom 13.09.2026, Weg (a): „auf iOS wird IMMER gemerkt".</b>
        /// <see cref="IProjektKontext"/> kennt nur <c>Uebernehmen</c>; das Setzen ohne
        /// Fortschreiben von <c>Tab_Applikation</c> ist eine Zusage von
        /// <see cref="ProjektKontextCtrl"/> allein. Unter Windows liegt genau der in
        /// <c>Dienste.Projekt</c> (<c>Program.cs:146-147</c>) — der Weg ist damit Wort
        /// für Wort der bisherige, die zwei Startkacheln merken weiterhin und die zwei
        /// Menüwege weiterhin nicht. Eine Schale mit einem anderen Träger
        /// (<c>EPOS.iOS/Dienste/IosProjektKontext</c> reicht auf denselben Kern durch,
        /// ist aber nicht derselbe Typ) übernimmt und merkt damit immer. Das ist
        /// gewollt: Die Windows-Unterscheidung trennt ZWEI Einstiege, und die gibt es
        /// auf iOS nicht — die Regel hätte dort keinen Gegenstand. <b>
        /// <see cref="IProjektKontext"/> wird dafür ausdrücklich NICHT um
        /// <c>Setzen</c> erweitert</b>; eine Windows-Bedienregel gehört nicht in die
        /// geteilte Dienste-Schnittstelle.</para>
        /// </summary>
        private static void ProjektkontextNachziehen(bool merken)
        {
            string name = WizardCtrl.Aktueller?.Projektname ?? "";
            if (name == "") return;

            if (!merken && Dienste.Projekt is ProjektKontextCtrl kern)
            {
                kern.Setzen(name);
                return;
            }

            Dienste.Projekt.Uebernehmen(0, name);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        /// <summary>Die Projekte des linken Bandes — dieselbe Liste wie in W15a.</summary>
        private static IReadOnlyList<ProjektKopfZeile> Projektliste()
        {
            try { return ProjektCtrl.NamenListe(); }
            catch (Exception ex)
            {
                Console.WriteLine("Projektliste konnte nicht gelesen werden: " + ex.Message);
                return new List<ProjektKopfZeile>();
            }
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
