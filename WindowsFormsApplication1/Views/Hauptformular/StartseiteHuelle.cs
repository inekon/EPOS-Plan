using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Start;
using EPOS.UI.Dialoge.Projekt;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Startseite (iU9-W16b.3) — sie löst
    /// <c>Views/Hauptformular/Form_Start</c> ab (2 300 Z. + 1 381 Designer).
    ///
    /// <para><b>Sie ist die NICHT-MODALE Hülle</b> (<see cref="BlazorSeite{T}"/>)
    /// und sitzt unmittelbar in <c>MDIMainForm</c> — dort, wo bis hierher die
    /// eingebettete <c>Form_Start</c> hing (<c>TopLevel = false</c>,
    /// <c>Dock = Fill</c>). Muster: <c>BerichteKostenHuelle</c> aus W5.6; wie dort
    /// gibt es KEIN <c>Oeffnen</c> — eine Seite wird nicht gezeigt, sie steht.</para>
    ///
    /// <para><b>Was hier liegt und was nicht.</b> Hier liegen die <b>21
    /// Kachelwege</b> — wörtlich die Handler von <c>Form_Start</c>: Liste lesen,
    /// Razor-Hülle öffnen, bei OK zurückschreiben. Was dort daneben stand — der
    /// Kachelanstrich, die Bitmaske, das Auffrischen einzelner Steuerelemente —
    /// steht nicht mehr: Die Seite liest ihren Bestand nach jedem Klick in EINEM
    /// Zug aus <see cref="KomponentenBestandCtrl"/> (E-3, Nachweis N6).</para>
    ///
    /// <para><b>Der Projektkontext liegt im Kern</b> (<see cref="ProjektKontextCtrl"/>,
    /// K2). Die Hülle hängt sich an sein Ereignis <c>Gewechselt</c> und meldet den
    /// Wechsel über den <see cref="SeitenZustand"/> an die Seite weiter — die
    /// WebView bleibt dabei stehen (Risiko R5).</para>
    /// </summary>
    internal sealed class StartseiteHuelle
    {
        private readonly SeitenZustand _zustand = new SeitenZustand();
        private readonly Func<IWin32Window> _besitzer;
        private readonly ProjektKontextCtrl _kontext;

        private BerichteKostenHuelle _berichte;

        /// <summary>Die Weiche der Solarthermiekachel (Profil / Ganglinie).</summary>
        private bool _solarGanglinie;

        /// <summary>Ein einmaliger Kurzhinweis für die Seite; leer = keiner.</summary>
        private string _kurzhinweis = "";

        /// <summary>
        /// Die zwei Bedarfsrechnungen des offenen Projekts (Befund W16-B29,
        /// Entscheid E-5). <c>Form_Start</c> besaß sie als zwei Felder und reichte
        /// sie an die Ergebnisansicht durch — genau das machte deren Fenster modal.
        /// Sie gehören jetzt dem PROJEKT und werden bei einem Wechsel verworfen.
        /// </summary>
        private readonly BedarfsZustand _bedarf = new BedarfsZustand();

        /// <summary>
        /// Die zuletzt gebaute Hülle — der Weg der übrigen Windows-Wege an die
        /// Seite. <c>Form_Start</c> war dafür <c>Program.startfrm</c>; ein
        /// statischer Halter ist dasselbe Muster (und dieselbe Zahl von
        /// Instanzen: eine).
        /// </summary>
        internal static StartseiteHuelle Aktuelle { get; private set; }

        internal StartseiteHuelle(Func<IWin32Window> besitzer, ProjektKontextCtrl kontext)
        {
            _besitzer = besitzer;
            _kontext = kontext ?? throw new ArgumentNullException(nameof(kontext));

            _kontext.Gewechselt += ProjektGewechselt;
            Aktuelle = this;
        }

        // =====================================================================
        //  Der Parametersatz der Seite
        // =====================================================================

        /// <summary>Der Parametersatz der Razor-Startseite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                [SeitenZustand.PARAMETER] = _zustand,

                ["Kacheln"] = new Func<IReadOnlyList<StartKachel>>(Kacheln),
                ["Geklickt"] = new Action<string>(Kachelweg),
                ["ProjektId"] = new Func<int>(() => _kontext.Id),
                ["Varianten"] = new Func<IReadOnlyList<(int Id, string Name)>>(Varianten),
                ["VarianteGewaehlt"] = new Action<int>(VarianteWechseln),
                ["Klimaregionen"] = new Func<IReadOnlyList<string>>(StartseiteCtrl.Klimaregionen),
                ["Klimaregion"] = new Func<string>(() => _kontext.Klimazone),
                ["KlimaSpeichern"] = new Func<string, (bool Fehler, string Text)>(KlimaSpeichern),
                ["Bericht"] = new Func<Zusammenfassung>(Zusammenfassen),
                ["SolarartGewaehlt"] = new Action<bool>(an => _solarGanglinie = an),
                ["Kurzhinweis"] = new Func<string>(KurzhinweisAbholen),
                ["BerichteGaben"] = Berichte.Gaben(),

                // E-5: Die zwei Simulationsansichten bleiben IN dieser WebView -
                // die Konfiguration als freie Ansicht, das Ergebnis als
                // Ueberlagerung. Die beiden modalen Huellen sind damit entfallen.
                ["SimulationKonfigGaben"] =
                    new Func<IReadOnlyDictionary<string, object>>(
                        () => SimulationKonfigHuelle.Gaben(_kontext.Id)),
                ["SimulationErgebnisGaben"] =
                    new Func<IReadOnlyDictionary<string, object>>(
                        () => SimulationErgebnisHuelle.Gaben(
                                  () => _besitzer?.Invoke() as Form, _kontext.Id, _bedarf)),
                ["ErgebnisTitelText"] = MyResource.Resource.SIMERG_TITEL,

                // ---- Texte (die 17 MyResource-Schluessel von Form_Start und die
                //      78 neuen aus seinen drei .resx, iU9-W16b.2) --------------
                ["GattungText"] = MyResource.Resource.START_GATTUNG,
                ["ProjektLabelText"] = MyResource.Resource.START_LBL_PROJEKT,
                ["PlatzhalterText"] = MyResource.Resource.Text_Select,
                ["KlimaLabelText"] = MyResource.Resource.START_LBL_KLIMA,
                ["KlimaRegionText"] = MyResource.Resource.START_KLIMA_REGION,
                ["KlimaPlatzhalterText"] = MyResource.Resource.START_KLIMA_PLATZHALTER,
                ["KlimaHinweisText"] = MyResource.Resource.START_KLIMA_HINWEIS,
                ["KlimaSpeichernText"] = MyResource.Resource.WIZ_BTN_SPEICHERN,
                ["StatusOffenText"] = MyResource.Resource.START_STATUS_OFFEN,
                ["StatusKeinsText"] = MyResource.Resource.START_STATUS_KEINS,
                ["SperreText1"] = MyResource.Resource.Text_Form_Start_MessageBox1,
                ["SperreText2"] = MyResource.Resource.Text_Form_Start_MessageBox2,

                ["ReiterProjektText"] = MyResource.Resource.START_REITER_PROJEKT,
                ["ReiterWaermeText"] = MyResource.Resource.START_REITER_WAERME,
                ["ReiterStromText"] = MyResource.Resource.START_REITER_STROM,
                ["ReiterErzeugerText"] = MyResource.Resource.START_REITER_ERZEUGER,
                ["ReiterSimulationText"] = MyResource.Resource.START_REITER_SIMULATION,
                ["ReiterBerichteText"] = MyResource.Resource.START_REITER_BERICHTE,

                ["ProjektKopfText"] = MyResource.Resource.START_P_KOPF,
                ["ProjektText"] = MyResource.Resource.START_P_TEXT,

                ["WaermeKopfText"] = MyResource.Resource.START_W_KOPF,
                ["WaermeText"] = MyResource.Resource.START_W_TEXT,
                ["WaermeHinweisTitelText"] = MyResource.Resource.START_W_HINWEIS_TITEL,
                ["WaermeHinweisText"] = MyResource.Resource.START_W_HINWEIS,

                ["StromKopfText"] = MyResource.Resource.START_S_KOPF,
                ["StromText"] = MyResource.Resource.START_S_TEXT,

                ["ErzeugerKopfText"] = MyResource.Resource.START_E_KOPF,
                ["ErzeugerText"] = MyResource.Resource.START_E_TEXT,
                ["ErzeugerTippTitelText"] = MyResource.Resource.START_E_TIPP_TITEL,
                ["ErzeugerTippText"] = MyResource.Resource.START_E_TIPP,
                ["SolarProfilText"] = MyResource.Resource.START_E_PROFIL,
                ["SolarGanglinieText"] = MyResource.Resource.START_E_GANGLINIE,

                ["SimulationKopfText"] = MyResource.Resource.START_SIM_KOPF,
                ["SimulationText"] = MyResource.Resource.START_SIM_TEXT,
                ["SimulationKonfigText"] = MyResource.Resource.START_SIM_BTN_KONFIG,
                ["ZusammenfassungTitelText"] = MyResource.Resource.START_SIM_ZUSAMMENFASSUNG,
                ["ZusammenfassungProjektnameText"] = MyResource.Resource.START_SIM_PROJEKTNAME,
                ["ZusammenfassungWaermebedarfText"] = MyResource.Resource.START_SIM_WBEDARF,
                ["ZusammenfassungStrombedarfText"] = MyResource.Resource.START_SIM_STROMBEDARF,
                ["ZusammenfassungKomponentenText"] = MyResource.Resource.START_SIM_KOMPONENTEN,

                ["KeineKlimaregionText"] = MyResource.Resource.Text_Form_Start_KlimaregionNichtGesetzt,
                ["ZurueckText"] = MyResource.Resource.START_BTN_ZURUECK,
                ["WeiterText"] = MyResource.Resource.START_BTN_WEITER
            };
        }

        /// <summary>Der geteilte Zustand — die Seitenhülle reicht ihn hinein.</summary>
        internal SeitenZustand Zustand { get { return _zustand; } }

        private BerichteKostenHuelle Berichte
        {
            get
            {
                if (_berichte == null)
                {
                    _berichte = new BerichteKostenHuelle(() => _besitzer?.Invoke() as Form);
                    _berichte.SetzeProjekt(_kontext.Id, _kontext.Name);
                }
                return _berichte;
            }
        }

        // =====================================================================
        //  Projektwechsel
        // =====================================================================

        /// <summary>
        /// Der Projektkontext hat gewechselt: Die Seite liest ihre Gaben neu, und
        /// der Reiter „Berichte &amp; Kosten" erfährt es ebenfalls — wörtlich das,
        /// was <c>Form_Start.ProjektKontextUebernehmen</c> am Ende tat
        /// (<c>VariantenAnzeigeAktualisieren</c>).
        /// </summary>
        private void ProjektGewechselt()
        {
            // E-5: Die zwei Bedarfsrechnungen gehoeren dem PROJEKT - ein Wechsel
            // macht sie hinfaellig.
            _bedarf.FuerProjekt(_kontext.Id);

            _berichte?.SetzeProjekt(_kontext.Id, _kontext.Name);

            _zustand.ProjektSetzen(_kontext.Id, _kontext.Name);
            _zustand.Auffrischen();
        }

        /// <summary>
        /// Zieht die Variantenanzeige nach, ohne dass das Projekt gewechselt hätte —
        /// der Ersatz für <c>Form_Start.VariantenAnzeigeAktualisieren</c>
        /// (Menü „Als Variante speichern…", <c>Ansichten.Varianten</c>).
        /// </summary>
        internal void VariantenAnzeigeAktualisieren()
        {
            _berichte?.SetzeProjekt(_kontext.Id, _kontext.Name);
            _zustand.Auffrischen();
        }

        /// <summary>
        /// Öffnet den Reiter „Berichte &amp; Kosten" auf der gewünschten Seite —
        /// der Ersatz für <c>Form_Start.ZeigeBerichteKosten</c> (Menü
        /// „Projekte › Varianten und Bericht…", <c>Ansichten.BerichteKosten</c>).
        /// </summary>
        internal void ZeigeBerichteKosten(string seite = null)
        {
            if (!string.IsNullOrEmpty(seite)) Berichte.ZeigeSeite(seite);
            _zustand.Auffrischen();
        }

        /// <summary>
        /// Der Kurzhinweis „Projekt &lt;Name&gt; geöffnet!" — der Rückweg des
        /// Assistenten (<c>AssistentHuelle</c>, W16a). Der Vorläufer öffnete dafür
        /// ein <c>Form_Hinweis</c> über der auslösenden Kachel; hier wird der Satz
        /// vorgemerkt und beim nächsten Auffrischen von der Seite abgeholt (Muster
        /// <c>BerichteKostenSeite.Seitenwunsch</c>).
        /// </summary>
        internal void HinweisProjektGeoeffnet()
        {
            _kurzhinweis = MyResource.Resource.Text_Projekt + " " + _kontext.Name + " "
                         + MyResource.Resource.Text_Geoeffnet + "!";
            _zustand.Auffrischen();
        }

        private string KurzhinweisAbholen()
        {
            string satz = _kurzhinweis;
            _kurzhinweis = "";
            return satz;
        }

        private void VarianteWechseln(int idProjekt)
        {
            // Woertlich comboBox_Varianten_SelectedIndexChanged (:2260-2271): Name
            // zur Id lesen, Kontext setzen. Ohne "zuletzt geoeffnet" - der Bestand
            // merkte sich einen Variantenwechsel ausdruecklich NICHT.
            string name = StartseiteCtrl.Projektname(idProjekt);
            if (!string.IsNullOrEmpty(name)) _kontext.Setzen(name);
        }

        private IReadOnlyList<(int Id, string Name)> Varianten()
        {
            List<(int, string)> liste = new List<(int, string)>();
            foreach (VariantenEintrag e in StartseiteCtrl.Varianten(_kontext.Id).Eintraege)
                liste.Add((e.Id, e.Name));
            return liste;
        }

        private (bool Fehler, string Text) KlimaSpeichern(string region)
        {
            // Woertlich btn_Speichern_Click (:1856-1896) - die fuenf MessageBox
            // werden EIN Banner, die Schluessel bleiben dieselben.
            switch (StartseiteCtrl.KlimaregionSpeichern(_kontext.Id, _kontext.Name, region))
            {
                case KlimaStand.KeinProjekt:
                    return (true, MyResource.Resource.Text_Form_Start_MessageBox1);
                case KlimaStand.KeineRegion:
                    return (true, MyResource.Resource.Text_Form_Start_KlimaregionAuswaehlen);
                case KlimaStand.RegionNichtGefunden:
                    return (true, MyResource.Resource.Text_Form_Start_KlimaregionNichtGefunden);
                case KlimaStand.NichtUebernommen:
                    return (true, MyResource.Resource.Text_Form_Start_KlimaregionNichtUebernommen);
                default:
                    // Der Kontext fuehrt die Klimazone; nach dem Schreiben neu lesen.
                    _kontext.Setzen(_kontext.Name);
                    return (false, MyResource.Resource.Text_Form_Start_KlimaregionGespeichert);
            }
        }

        // =====================================================================
        //  Der Reiter "Simulation" - die Zusammenfassung
        // =====================================================================

        /// <summary>
        /// Wörtlich <c>Form_Start.tabPage5_Enter</c> (:1062-1093): Ohne gesetzte
        /// Klimaregion gibt es keine Zusammenfassung (<c>null</c>) — die Seite
        /// springt dann auf Reiter 1 zurück und meldet.
        /// </summary>
        private Zusammenfassung Zusammenfassen()
        {
            int idKlima = StartseiteCtrl.KlimaregionIdVonProjekt(_kontext.Name);
            if (idKlima == 0) return null;

            _bedarf.FuerProjekt(_kontext.Id);

            _bedarf.Strom.Berechnung(_kontext.Id);
            _bedarf.Waerme.Waermebedarf_berechnen(_kontext.Id, idKlima);

            return new Zusammenfassung(
                _kontext.Name,
                _bedarf.Waerme.Waermebedarf_Gesamt.ToString("F2") + " MWh/a",
                _bedarf.Strom.Strombedarf_gesamt.ToString("F2") + " MWh/a",
                Technologien());
        }

        /// <summary>
        /// Die gewählten Technologien als Satz — wörtlich <c>tabPage5_Enter</c>
        /// (:1087-1092), nur aus der Bitmaske des Kerns statt aus dem Feld
        /// <c>status</c> der Maske.
        /// </summary>
        private string Technologien()
        {
            int status = KomponentenBestandCtrl.Lesen(_kontext.Id).Bitmaske;
            string text = "";

            if ((status & 1) == 1) text += MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL;
            if ((status & 2) == 2) text += ", " + MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE;
            if ((status & 4) == 4) text += ", " + MyResource.Resource.SIM_STROMSPEICHER;
            if ((status & 256) == 256) text += ", " + MyResource.Resource.SIM_ERZEUGERNAME_BHKW;

            if (text.StartsWith(", ")) text = text.Substring(2);
            return text;
        }

        // =====================================================================
        //  Die 21 Kacheln
        // =====================================================================

        /// <summary>
        /// Die 21 Kacheln mit Beschriftung und Bestand. Der Statuspunkt kommt aus
        /// der EINEN Bitmaske des Kerns (<see cref="KomponentenBestandCtrl"/>) —
        /// <c>Form_Start.UpdateWizardSymbole</c> mit seinen dreizehn Bits, sieben
        /// Inline-SQL und sechs <c>ReadAllFilter</c> ist damit ersatzlos entfallen
        /// (Entscheid E-3, Nachweis N6).
        /// </summary>
        private IReadOnlyList<StartKachel> Kacheln()
        {
            int status = _kontext.Id > 0
                ? KomponentenBestandCtrl.Lesen(_kontext.Id).Bitmaske
                : 0;

            Kachelstand Stand(int bit) => (status & bit) == bit ? Kachelstand.An : Kachelstand.Aus;

            return new List<StartKachel>
            {
                Ohne(Kachelschluessel.ProjektNeu, Reiterschluessel.Projekt,
                     MyResource.Resource.START_K_NEU_T, MyResource.Resource.START_K_NEU_B),
                Ohne(Kachelschluessel.ProjektOeffnen, Reiterschluessel.Projekt,
                     MyResource.Resource.START_K_OEFFNEN_T, MyResource.Resource.START_K_OEFFNEN_B),
                Ohne(Kachelschluessel.ProjektZuletzt, Reiterschluessel.Projekt,
                     MyResource.Resource.START_K_ZULETZT_T, MyResource.Resource.START_K_ZULETZT_B),
                Ohne(Kachelschluessel.ProjektSpeichernUnter, Reiterschluessel.Projekt,
                     MyResource.Resource.START_K_SPEICHERNUNTER_T, MyResource.Resource.START_K_SPEICHERNUNTER_B),
                Ohne(Kachelschluessel.ProjektLoeschen, Reiterschluessel.Projekt,
                     MyResource.Resource.START_K_LOESCHEN_T, MyResource.Resource.START_K_LOESCHEN_B),

                Mit(Kachelschluessel.Gebaeude, Reiterschluessel.Waermebedarf,
                    MyResource.Resource.START_K_GEBAEUDE_T, MyResource.Resource.START_K_GEBAEUDE_B, Stand(8)),
                Mit(Kachelschluessel.WaermebedarfDaten, Reiterschluessel.Waermebedarf,
                    MyResource.Resource.START_K_WBDATEN_T, MyResource.Resource.START_K_WBDATEN_B, Stand(16)),
                Mit(Kachelschluessel.Prozesswaerme, Reiterschluessel.Waermebedarf,
                    MyResource.Resource.START_K_PROZESS_T, MyResource.Resource.START_K_PROZESS_B, Stand(32)),
                Mit(Kachelschluessel.Brauchwasser, Reiterschluessel.Waermebedarf,
                    MyResource.Resource.START_K_BRAUCHWASSER_T, MyResource.Resource.START_K_BRAUCHWASSER_B, Stand(4096)),

                Mit(Kachelschluessel.StromStandardprofil, Reiterschluessel.Strombedarf,
                    MyResource.Resource.START_K_STDLAST_T, MyResource.Resource.START_K_STDLAST_B, Stand(64)),
                // "Eigenes Profil" fuehrt in die STAMMDATEN und nicht in das Projekt -
                // der Bestand malte an dieser Kachel folgerichtig keinen Statuspunkt.
                Ohne(Kachelschluessel.StromEigenesProfil, Reiterschluessel.Strombedarf,
                     MyResource.Resource.START_K_EIGENESPROFIL_T, MyResource.Resource.START_K_EIGENESPROFIL_B),
                Mit(Kachelschluessel.StromMessdaten, Reiterschluessel.Strombedarf,
                    MyResource.Resource.START_K_MESSDATEN_T, MyResource.Resource.START_K_MESSDATEN_B, Stand(128)),

                Mit(Kachelschluessel.Waermepumpe, Reiterschluessel.Erzeuger,
                    MyResource.Resource.START_K_WP_T, MyResource.Resource.START_K_WP_B, Stand(2)),
                Mit(Kachelschluessel.Heizkessel, Reiterschluessel.Erzeuger,
                    MyResource.Resource.START_K_HEIZKESSEL_T, MyResource.Resource.START_K_HEIZKESSEL_B, Stand(1)),
                Mit(Kachelschluessel.Solarthermie, Reiterschluessel.Erzeuger,
                    MyResource.Resource.START_K_SOLAR_T, MyResource.Resource.START_K_SOLAR_B, Stand(512)),
                Mit(Kachelschluessel.Bhkw, Reiterschluessel.Erzeuger,
                    MyResource.Resource.START_K_BHKW_T, MyResource.Resource.START_K_BHKW_B, Stand(256)),
                Mit(Kachelschluessel.Photovoltaik, Reiterschluessel.Erzeuger,
                    MyResource.Resource.START_K_PV_T, MyResource.Resource.START_K_PV_B, Stand(1024)),
                Mit(Kachelschluessel.Stromspeicher, Reiterschluessel.Erzeuger,
                    MyResource.Resource.START_K_STROMSPEICHER_T, MyResource.Resource.START_K_STROMSPEICHER_B, Stand(4)),
                Mit(Kachelschluessel.Pufferspeicher, Reiterschluessel.Erzeuger,
                    MyResource.Resource.START_K_PUFFER_T, MyResource.Resource.START_K_PUFFER_B, Stand(2048)),

                Ohne(Kachelschluessel.SimulationKonfiguration, Reiterschluessel.Simulation,
                     MyResource.Resource.START_SIM_BTN_KONFIG, ""),
                Ohne(Kachelschluessel.SimulationErgebnis, Reiterschluessel.Simulation,
                     MyResource.Resource.START_K_DETAILSIM_T, MyResource.Resource.START_K_DETAILSIM_B)
            };
        }

        private static StartKachel Ohne(string schluessel, string reiter, string titel, string text)
            => new StartKachel { Schluessel = schluessel, Reiter = reiter, Titel = titel, Beschreibung = text };

        private static StartKachel Mit(string schluessel, string reiter, string titel, string text,
                                       Kachelstand stand)
            => new StartKachel
            {
                Schluessel = schluessel, Reiter = reiter, Titel = titel,
                Beschreibung = text, Zustand = stand
            };

        // =====================================================================
        //  Die 21 Kachelwege - woertlich die Handler von Form_Start
        // =====================================================================

        /// <summary>
        /// Der EINE Verteiler. Der Vorläufer hatte drei: ein Wörterbuch mit 24
        /// Einträgen samt <c>CentralControl_Click</c>, vierzehn einzeilige
        /// Weiterleitungshandler und sechs unmittelbar verdrahtete
        /// <c>AktionsKarte.Geklickt</c> (Befund W16-B19).
        /// </summary>
        private void Kachelweg(string schluessel)
        {
            IWin32Window wirt = _besitzer?.Invoke();

            switch (schluessel)
            {
                case Kachelschluessel.ProjektNeu: ProjektNeu(); break;
                case Kachelschluessel.ProjektOeffnen: ProjektOeffnen(); break;
                case Kachelschluessel.ProjektZuletzt: ProjektZuletzt(wirt); break;
                case Kachelschluessel.ProjektSpeichernUnter: new MenueCtrl().ProjektSpeichernUnter(); break;
                case Kachelschluessel.ProjektLoeschen: ProjektLoeschen(); break;

                case Kachelschluessel.Gebaeude: Gebaeude(wirt); break;
                case Kachelschluessel.WaermebedarfDaten: WaermebedarfDaten(wirt); break;
                case Kachelschluessel.Prozesswaerme: Prozesswaerme(wirt); break;
                case Kachelschluessel.Brauchwasser: Brauchwasser(wirt); break;

                case Kachelschluessel.StromStandardprofil: Standardlastprofil(wirt); break;
                case Kachelschluessel.StromEigenesProfil:
                    TypStammHuelle.ProfilOeffnen(wirt, BedarfsArt.Stromverbraucher); break;
                case Kachelschluessel.StromMessdaten: Stromganglinie(wirt); break;

                case Kachelschluessel.Waermepumpe: Waermepumpe(wirt); break;
                case Kachelschluessel.Heizkessel: Heizkessel(wirt); break;
                case Kachelschluessel.Solarthermie: Solarthermie(wirt); break;
                case Kachelschluessel.Bhkw: Bhkw(wirt); break;
                case Kachelschluessel.Photovoltaik: Photovoltaik(wirt); break;
                case Kachelschluessel.Stromspeicher: Stromspeicher(wirt); break;
                case Kachelschluessel.Pufferspeicher: Pufferspeicher(wirt); break;

                // E-5: Die zwei Simulationswege beantwortet die SEITE selbst - sie
                // holt sich ihren Parametersatz ueber SimulationKonfigGaben bzw.
                // SimulationErgebnisGaben und wechselt die Ansicht. Hier kommen sie
                // deshalb gar nicht mehr an.
                case Kachelschluessel.SimulationKonfiguration:
                case Kachelschluessel.SimulationErgebnis: break;
            }
        }

        // ---- Reiter 1: Projekt ---------------------------------------------

        private void ProjektNeu()
        {
            // Woertlich pBox_ProjektNeu_Click (:289-310).
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektNeu();

            if (Program.wizardctrl == null || Program.wizardctrl.Projektname == "") return;

            // Zuletzt geoeffnetes Projekt merken - Schreiblogik unveraendert; sie
            // liegt seit W16b.0 im Kern.
            _kontext.Uebernehmen(0, Program.wizardctrl.Projektname);
        }

        private void ProjektOeffnen()
        {
            // Woertlich pBox_ProjektOeffnen_Click (:331-352).
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektBearbeiten();

            if (Program.wizardctrl != null && Program.wizardctrl.Projektname != "")
                _kontext.Uebernehmen(0, Program.wizardctrl.Projektname);
        }

        private void ProjektZuletzt(IWin32Window wirt)
        {
            // Woertlich pBox_ProjektZuletzt_Click (:746-777).
            (string Name, int Id) gemerkt = ProjektKontextCtrl.ZuletztGeoeffnet();
            string gewaehlt = gemerkt.Name ?? "";

            if (gewaehlt.Trim() == "" || !_kontext.Uebernehmen(0, gewaehlt))
            {
                // Rueckfall: nichts gemerkt oder das gemerkte Projekt existiert nicht
                // mehr - dann (und nur dann) die Projektliste zeigen.
                if (!ProjektWahlHuelle.Oeffnen(wirt, ProjektWahlDialog.ProjektZweck.Oeffnen,
                                               out ProjektKopfZeile wahl,
                                               vorauswahl: gewaehlt,
                                               zuletztGeaendertZuerst: true)) return;
                gewaehlt = wahl.Name ?? "";

                if (gewaehlt == "" || !_kontext.Uebernehmen(0, gewaehlt))
                {
                    Dienste.Dialog.Meldung(MyResource.Resource.Text_Form_Start_ProjektGeloescht);
                    return;
                }
            }

            HinweisProjektGeoeffnet();
        }

        private void ProjektLoeschen()
        {
            // Woertlich pBox_Delete_Click (:1199-1216): War es das offene Projekt,
            // steht die Startseite danach auf "keins".
            MenueCtrl menu = new MenueCtrl();
            string szProjekt = menu.ProjektDelete();

            if (!string.IsNullOrEmpty(szProjekt) && szProjekt == _kontext.Name)
                _kontext.Leeren();
        }

        // ---- Reiter 2: Waermebedarf ----------------------------------------

        private void Gebaeude(IWin32Window wirt)
        {
            // Woertlich pBox_Gebaude_Click (:264-287).
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(_kontext.Id);

            if (GebaeudeHuelle.Oeffnen(wirt, _kontext.Id, _kontext.Name, liste))
            {
                wizctrl.Del_Projekt_ZuordungGebäude(_kontext.Id);
                wizctrl.Add_Projekt_ZuordungGebäude(_kontext.Id, liste);

                projctrl.ReadSingle(_kontext.Name);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void WaermebedarfDaten(IWin32Window wirt)
        {
            // Woertlich pBox_WBedarfDaten_Click (:239-262).
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            List<Z_ProjWaermebedarfModel> liste = Z_ProjektGebGanglinieCtrl.LiesProjekt(_kontext.Id);

            if (WaermebedarfExternHuelle.Oeffnen(wirt, _kontext.Id, _kontext.Name, liste))
            {
                wizctrl.Del_WaermebedarfExtern(_kontext.Id);
                wizctrl.Add_WaermebedarfExtern(_kontext.Id, liste);
                projctrl.ReadSingle(_kontext.Name);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void Prozesswaerme(IWin32Window wirt)
        {
            // Woertlich pBox_Prozess_Click (:213-237).
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            List<Z_ProjektProzesswaermeModel> liste = Z_ProjektProzesswaermeCtrl.LiesProjekt(_kontext.Id);

            if (BedarfsProfileHuelle.Oeffnen(wirt, _kontext.Id, _kontext.Name, liste))
            {
                wizctrl.Del_Projekt_Prozess(_kontext.Id);
                wizctrl.Add_Projekt_Prozess(_kontext.Id, liste);

                projctrl.ReadSingle(_kontext.Name);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void Brauchwasser(IWin32Window wirt)
        {
            // Woertlich pBox_Brauchwasser_Click (:1755-1779).
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            List<Z_ProjektBrauchwasserModel> liste = Z_ProjektBrauchwasserCtrl.LiesProjekt(_kontext.Id);

            if (BedarfsProfileHuelle.Oeffnen(wirt, _kontext.Id, _kontext.Name, liste))
            {
                wizctrl.Del_Projekt_Brauchwasser(_kontext.Id);
                wizctrl.Add_Projekt_Brauchwasser(_kontext.Id, liste);

                projctrl.ReadSingle(_kontext.Name);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        // ---- Reiter 3: Strombedarf -----------------------------------------

        private void Standardlastprofil(IWin32Window wirt)
        {
            // Woertlich pBox_StdLastProfil_Click (:414-439).
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            List<Z_ProjektStromverbraucherModel> liste =
                Z_ProjektStromverbraucherCtrl.LiesProjekt(_kontext.Id);

            if (BedarfsProfileHuelle.Oeffnen(wirt, _kontext.Id, _kontext.Name, liste))
            {
                wizctrl.Del_Projekt_Stromverbraucher(_kontext.Id);
                wizctrl.Add_Projekt_Stromverbraucher(_kontext.Id, liste);

                projctrl.ReadSingle(_kontext.Name);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void Stromganglinie(IWin32Window wirt)
        {
            // Woertlich pBox_StromMessdaten_Click (:447-477).
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            List<Z_ProjektStromganglinieModel> liste =
                Z_ProjektStromganglinieCtrl.LiesProjekt(_kontext.Id);

            if (StromganglinieHuelle.Oeffnen(wirt, _kontext.Id, liste))
            {
                wizctrl.Del_Stromganglinie(_kontext.Id);
                wizctrl.Add_Stromganglinie(_kontext.Id, liste);

                projctrl.ReadSingle(_kontext.Name);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        // ---- Reiter 4: Energieerzeuger -------------------------------------

        /// <summary>
        /// Die Anlagen EINES Typs — die vier Zeilen, die in acht Kachelhandlern
        /// wortgleich standen (<c>ReadAllFilter</c> + Schleife).
        /// </summary>
        private List<WErzeugerModel> Anlagen(int idType)
        {
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            werzctrl.ReadAllFilter("ID_Projekt=" + _kontext.Id + " and ID_Type=" + idType);

            List<WErzeugerModel> liste = new List<WErzeugerModel>();
            for (int i = 0; i < werzctrl.rows; i++) liste.Add(werzctrl.items[i]);
            return liste;
        }

        private void Schreiben(int idType, List<WErzeugerModel> liste)
        {
            WizardCtrl wizctrl = new WizardCtrl();
            wizctrl.Del_Projekt_Waermeerzeuger(_kontext.Id, idType);
            wizctrl.Add_WP_Waermeerzeuger(_kontext.Id, liste);
        }

        private void Aenderungsdatum()
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            projctrl.ReadSingle(_kontext.Name);
            projctrl.m_Aenderungsdatum = DateTime.Now;
            projctrl.Update();
        }

        private void Waermepumpe(IWin32Window wirt)
        {
            // Woertlich pBox_WP_Click (:479-505).
            List<WErzeugerModel> liste = Anlagen(WizardItemClass.WP_TYP);
            if (WaermepumpenHuelle.Oeffnen(wirt, _kontext.Id, liste))
                Schreiben(WizardItemClass.WP_TYP, liste);
        }

        private void Heizkessel(IWin32Window wirt)
        {
            // Woertlich pBox_Heizkessel_Click (:508-547).
            List<WErzeugerModel> liste = Anlagen(WizardItemClass.KESSEL_TYP);
            if (HeizkesselHuelle.Oeffnen(wirt, _kontext.Id, WizardItemClass.KESSEL_TYP, liste))
            {
                Schreiben(WizardItemClass.KESSEL_TYP, liste);
                Aenderungsdatum();
            }
        }

        private void Bhkw(IWin32Window wirt)
        {
            // Woertlich pBox_BHKW_Click (:1096-1125).
            List<WErzeugerModel> liste = Anlagen(WizardItemClass.BHKW_TYP);
            if (BhkwHuelle.Oeffnen(wirt, _kontext.Id, WizardItemClass.BHKW_TYP, liste))
                Schreiben(WizardItemClass.BHKW_TYP, liste);
        }

        private void Photovoltaik(IWin32Window wirt)
        {
            // Woertlich pBox_PV_Click (:1374-1412).
            List<WErzeugerModel> liste = Anlagen(WizardItemClass.PV_TYP);
            if (PhotovoltaikHuelle.Oeffnen(wirt, _kontext.Id, WizardItemClass.PV_TYP, liste))
            {
                Schreiben(WizardItemClass.PV_TYP, liste);
                Aenderungsdatum();
            }
        }

        private void Stromspeicher(IWin32Window wirt)
        {
            // Woertlich pBox_Stromspeicher_Click (:549-577).
            List<WErzeugerModel> liste = Anlagen(WizardItemClass.SP_TYP);
            if (StromspeicherHuelle.Oeffnen(wirt, _kontext.Id, WizardItemClass.SP_TYP, liste))
                Schreiben(WizardItemClass.SP_TYP, liste);
        }

        private void Pufferspeicher(IWin32Window wirt)
        {
            // Woertlich pBox_Pufferspeicher_Click (:1647-1676).
            List<WErzeugerModel> liste = Anlagen(WizardItemClass.PUFFER_TYP);
            if (PufferspeicherHuelle.Oeffnen(wirt, _kontext.Id, WizardItemClass.PUFFER_TYP, liste))
            {
                Schreiben(WizardItemClass.PUFFER_TYP, liste);
                // B0-6a: Im Dialog entfernte Puffer hinterlassen sonst Waisen.
                new PufferSpCtrl().ProjektWaisenEntfernen(_kontext.Id);
            }
        }

        private void Solarthermie(IWin32Window wirt)
        {
            // Woertlich pBox_Solarthermie_Click (:1250-1326): Die WEICHE entscheidet,
            // welcher der beiden Dialoge aufgeht. Das Einfaerben der zwei
            // Auswahlknoepfe (:1313-1325) ENTFAELLT - es sagte dasselbe wie der
            // Statuspunkt der Kachel (Bit 512).
            if (!_solarGanglinie)
            {
                List<WErzeugerModel> liste = Anlagen(WizardItemClass.SOLAR_TYP);
                if (SolarkollektorHuelle.Oeffnen(wirt, _kontext.Id, liste))
                {
                    Schreiben(WizardItemClass.SOLAR_TYP, liste);
                    Aenderungsdatum();
                }
                return;
            }

            List<Z_ProjektSolarganglinieModel> ganglinien =
                Z_ProjektSolarganglinieCtrl.LiesProjekt(_kontext.Id);

            if (SolarganglinieHuelle.Oeffnen(wirt, _kontext.Id, ganglinien))
            {
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Solarganglinie(_kontext.Id);
                wizctrl.Add_Solarganglinie(_kontext.Id, ganglinien);
                Aenderungsdatum();
            }
        }

    }
}
