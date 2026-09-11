using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Projektassistenten (iU9-W16a.5) — sie löst
    /// <c>WizardParent</c> ab.
    ///
    /// <para><b>Sie ist die Datenseite EINES Assistentenlaufs.</b> Der Ablauf steht
    /// im Kern (<see cref="AssistentCtrl"/>, K3), der Rahmen als Baustein
    /// <c>Assistent</c> und die dreizehn Seiten als <c>AssistentSeite</c>; hier
    /// werden die dreizehn Parametersätze gebaut — dieselben <c>Gaben</c>-Wörterbücher,
    /// die die elf Hüllen schon für <c>BlazorAssistentSeite</c> lieferten.</para>
    ///
    /// <para><b>Seit W16a-E-1 / W16b-O-5 ist der Assistent eine FREIE ANSICHT</b>
    /// (Aufgabe #62b, 11.09.2026) — es gibt kein <c>BlazorDialogForm</c> mehr und
    /// kein <c>DialogResult</c>. Er war die letzte Fachseite in einer modalen Hülle,
    /// und modal war er aus zwei Gründen: Seine Aufrufer werteten aus, OB gespeichert
    /// wurde, und der Rahmen zog danach den Projektkontext nach. Beides ist jetzt
    /// anders gelöst — der Nachzug geschieht HIER, unmittelbar nach dem gelungenen
    /// Speicherlauf (<see cref="ProjektkontextNachziehen"/>), und die Startseite
    /// erfährt ihn über <c>ProjektKontextCtrl.Gewechselt</c> wie jeden anderen
    /// Projektwechsel.</para>
    ///
    /// <para><b>Der Rückweg „Projekt öffnen".</b> Er stand als
    /// <c>WizardParent.ProjektOeffnenUndSchliessen</c> (:940-960) im Rahmen: Projekt
    /// aktiv setzen, den Namen in <c>WizardCtrl</c> nachziehen, schließen und die
    /// Startmaske kurz melden lassen. Er steht jetzt hier; das SCHLIESSEN meldet seit
    /// #62b die Seite selbst (<c>Geschlossen(false)</c>), es gibt kein Fenster
    /// mehr, das zuzumachen wäre.</para>
    /// </summary>
    internal static class AssistentHuelle
    {
        /// <summary>
        /// Der PARAMETERSATZ EINES neuen Assistentenlaufs — der Ersatz für
        /// <c>Oeffnen(besitzer, betriebsart)</c> seit #62b.
        ///
        /// <para>Er legt den <see cref="AssistentCtrl"/> des Laufs an und reicht ihn
        /// über die Delegaten in <see cref="Gaben"/> an die Razor-Seite. Gerufen wird
        /// er von <c>AppWurzel</c>, sobald die Ansicht ASSISTENT betreten wird —
        /// einmal je Lauf, wie vorher <c>new AssistentCtrl()</c> je Fenster.</para>
        /// </summary>
        /// <param name="betriebsart">
        /// <see cref="AssistentCtrl.BETRIEBSART_NEU"/> oder <c>…_BEARBEITEN</c>.
        /// </param>
        internal static IReadOnlyDictionary<string, object> AnsichtGaben(int betriebsart)
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

            return new Dictionary<string, object>(Gaben(ctrl, merken));
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
        internal static IReadOnlyDictionary<string, object> Gaben(AssistentCtrl ctrl,
                                                                  bool merken = false)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));

            // Die geteilten Listen der dreizehn Seiten. Sie leben so lange wie der
            // Lauf; die Seiten bearbeiten sie an Ort und Stelle.
            List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile> komponenten =
                new List<EPOS.UI.Dialoge.Bedarf.KomponentenZeile>();

            string[] gewaehlterName = { "" };

            return new Dictionary<string, object>
            {
                ["Betriebsart"] = ctrl.Betriebsart,
                ["Projekte"] = Projektliste(),

                ["SeiteAktiv"] = new Func<int, bool>(ctrl.SeiteAktiv),

                ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>>(
                    nr => Seitengaben(ctrl, nr, komponenten, gewaehlterName[0])),

                ["SeiteVerlassen"] = new Action<int>(nr => SeiteVerlassen(ctrl, nr)),
                ["SeitePruefen"] = new Func<int, string>(nr => SeitePruefen(ctrl, nr)),

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

                ["ProjektOeffnen"] = new Action<int, string>(ProjektOeffnen),

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
                    // verboten, Regel (d) der Blazor-Huelle).
                    StartseiteHuelle.Aktuelle?.Kurzhinweis(
                        Text_("WIZ_GESPEICHERT", "Daten gespeichert"));
                    return null;
                }),

                // 62b-E-1: Woran der Assistent erkennt, dass es etwas zu verlieren
                // gibt. Die Antwort rechnet der Kern aus seinem Zustand.
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
        /// Ein Schritt wird verlassen — wörtlich <c>WizardParent.Next</c> (:275-283):
        /// Beim Verlassen des Projektkopfes wandern seine sieben Felder in den
        /// Projektsatz, und beim ERSTEN Durchgang laufen die sechs Ladewege.
        /// </summary>
        /// <summary>
        /// Nutzerauftrag 02.09.2026 (mit Merge 5 aus WizardParent.Next und Wizard_Projekt.Pruefe):
        /// Pflichtfelder und Namensdoppel der Projektseite beim VERLASSEN pruefen - vorher
        /// fiel es erst beim Speichern auf, viele Seiten spaeter. Liefert den Meldungstext
        /// oder null.
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

        private static void SeiteVerlassen(AssistentCtrl ctrl, int nr)
        {
            if (nr != WizardItemClass.PROJEKT_ITEM) return;

            ctrl.ProjektkopfUebernehmen();
            if (!ctrl.BereitsGeladen) ctrl.Laden(ctrl.Projekt.m_szProjektname);
        }

        /// <summary>
        /// Der Parametersatz EINER Seite. Die Zuordnung Nummer → Hülle ist die
        /// bitgleiche Übernahme von <c>AssistentSeiten.ERZEUGER</c>.
        /// </summary>
        private static IReadOnlyDictionary<string, object> Seitengaben(
            AssistentCtrl ctrl, int nr,
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

                case WizardItemClass.GEBAEUDE_ITEM:
                    return GebaeudeHuelle.Gaben(null, id, name, ctrl.Gebaeude,
                                                wizard: true, admin: false);

                case WizardItemClass.WAERMEBEDARF_ITEM:
                    return WaermebedarfExternHuelle.Gaben(null, id, name, ctrl.Waermebedarf,
                                                          wizard: true);

                case WizardItemClass.PROZESS_ITEM:
                    return BedarfsProfileHuelle.AssistentGabenProzess(id, ctrl.Prozess);

                case WizardItemClass.STROMSTD_ITEM:
                    return BedarfsProfileHuelle.AssistentGabenStrom(id, ctrl.Stromverbraucher);

                case WizardItemClass.STROMLASTGANG_ITEM:
                    return StromganglinieHuelle.Gaben(id, ctrl.Stromganglinie, wizard: true);

                case WizardItemClass.WP_ITEM:
                    return WaermepumpenHuelle.Gaben(null, id, ctrl.Erzeuger, wizard: true);

                case WizardItemClass.SOLAR_ITEM:
                    return SolarkollektorHuelle.ProjektGaben(id, ctrl.Erzeuger, wizard: true);

                case WizardItemClass.PV_ITEM:
                    return PhotovoltaikHuelle.Gaben(null, id, WizardItemClass.PV_TYP,
                                                    ctrl.Erzeuger, wizard: true);

                case WizardItemClass.SP_ITEM:
                    return StromspeicherHuelle.Gaben(null, id, WizardItemClass.SP_TYP,
                                                     ctrl.Erzeuger, wizard: true);

                case WizardItemClass.KESSEL_ITEM:
                    return HeizkesselHuelle.Gaben(null, id, WizardItemClass.KESSEL_TYP,
                                                  ctrl.Erzeuger, wizard: true);

                case WizardItemClass.BHKW_ITEM:
                    return BhkwHuelle.Gaben(null, id, WizardItemClass.BHKW_TYP,
                                            ctrl.Erzeuger, wizard: true);

                default:
                    return null;
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
        /// </summary>
        private static void ProjektOeffnen(int id, string name)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(name)) return;

            if (!Program.menuectrl.ProjektAktivSetzen(name, id)) return;

            // WizardCtrl.Aktueller.Projektname haelt den zuletzt GESPEICHERTEN Namen
            // und wird beim Start nicht geleert. Die Zeile stand hier, weil der
            // Nachzug nach dem ShowDialog aus diesem Feld las; sie bleibt, weil
            // ProjektkontextNachziehen denselben Weg geht.
            if (WizardCtrl.Aktueller != null) WizardCtrl.Aktueller.Projektname = name;

            // Der Kurzhinweis „Projekt X geoeffnet!" ueber der Startseite. In der
            // modalen Fassung wurde er nach dem ShowDialog gezeigt, weil Close()
            // den Rahmen nur ausblendete; jetzt wechselt die Ansicht unmittelbar
            // danach, und die Startseite holt den Satz beim naechsten Auffrischen ab.
            StartseiteHuelle.Aktuelle?.HinweisProjektGeoeffnet();
        }

        /// <summary>
        /// Der NACHZUG des Projektkontexts nach einem gelungenen Speicherlauf
        /// (Aufgabe #62b).
        ///
        /// <para>Bis dahin stand er zweimal hinter dem <c>ShowDialog</c>: in
        /// <c>HauptfensterHuelle.ProjektAssistent</c> (als <c>Setzen</c>) und in
        /// <c>StartseiteHuelle.ProjektNeu</c>/<c>…ProjektOeffnen</c> (als
        /// <c>Uebernehmen</c>). Ohne modale Rückkehr gibt es diesen Ort nicht mehr —
        /// also steht er hier, an der einen Stelle, die weiß, dass geschrieben
        /// wurde. Die Startseite erfährt den Wechsel über
        /// <c>ProjektKontextCtrl.Gewechselt</c> wie jeden anderen auch.</para>
        ///
        /// <para><paramref name="merken"/> trägt den EINEN Unterschied der zwei
        /// Einstiege: Die Startkacheln schreiben das Projekt als „zuletzt geöffnet"
        /// fort (<c>Uebernehmen</c>), die zwei Menüwege nicht (<c>Setzen</c>) —
        /// nachzulesen im Klassenkopf von <c>ProjektKontextCtrl</c>.</para>
        /// </summary>
        private static void ProjektkontextNachziehen(bool merken)
        {
            string name = WizardCtrl.Aktueller?.Projektname ?? "";
            if (name == "") return;

            if (merken) Program.projektkontext?.Uebernehmen(0, name);
            else Program.projektkontext?.Setzen(name);
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
