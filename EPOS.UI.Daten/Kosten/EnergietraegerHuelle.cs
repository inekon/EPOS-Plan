using System;
using System.Collections.Generic;
using System.Globalization;
using EPOS.UI.Dialoge.Kosten;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE der Energieträgerverwaltung (iU9-W4.4) — Nachfolge der
    /// gelöschten Masken <c>Views/Kosten/Form_Energietraeger</c> (535 Z.) und
    /// <c>Views/Kosten/ucFuelSettings</c> (2 103 Z.).
    ///
    /// <para><b>Sie ist plattformfrei</b> und liegt in <c>EPOS.UI.Daten</c>:
    /// Ihre Quellen sind Kern-Controller, sie kennt kein Fenster. Was Windows
    /// beisteuert, steht in <c>Views/Kosten/EnergietraegerFenster.cs</c> —
    /// eine <c>BlazorDialogForm</c> um <see cref="EnergietraegerDialog"/>,
    /// gebaut aus <see cref="Gaben"/>. Auf iOS zeigt dieselbe Hülle dieselbe
    /// Komponente ohne diesen Adapter.</para>
    ///
    /// <para><b>Hier liegt die Datenseite.</b> Die neun SQL-Anweisungen der
    /// Trägerkarte stehen seit dieser Welle im Kern-Controller
    /// <see cref="EnergietraegerPreisCtrl"/>; die Katalogpflege lief schon
    /// vorher über <see cref="EnergietraegerKatalogCtrl"/>. Diese Hülle lädt,
    /// rechnet und schreibt — die Komponenten
    /// <see cref="EnergietraegerDialog"/> und
    /// <see cref="EnergietraegerEinstellungen"/> zeigen nur an.</para>
    ///
    /// <para><b>Die Rechenwege bleiben, wo sie hingehören.</b> Der
    /// Einheitenprüfer (<see cref="EnergieEinheitenPruefung"/>) beantwortet
    /// weiter die Frage, ob eine Regel abgeschaltet werden darf und ob der
    /// Träger kWh erreicht; die Preisanteile rechnet
    /// <see cref="StrompreisZerlegungCtrl"/> bzw.
    /// <see cref="BrennstoffBestandteilCtrl"/>. Es gibt keine zweite Fassung
    /// einer Fachregel, nur einen zweiten Leser.</para>
    ///
    /// <para><b>Ein Fenster, eine WebView.</b> Kostenprofil, Spotpreis-Import,
    /// saisonale Sätze und der Emissionskatalog sind seit Welle 3
    /// Razor-Komponenten; sie erscheinen jetzt in einer <c>Ueberlagerung</c>
    /// desselben Fensters statt in einer zweiten <c>BlazorDialogForm</c>
    /// (Risiko R2).</para>
    /// </summary>
    public sealed class EnergietraegerHuelle
    {
        /// <summary>Wunschbreite des Fensters — die Plattformhülle macht daraus ihr Maß.</summary>
        public const int FENSTER_BREITE = 1140;

        /// <summary>Wunschhöhe des Fensters. Die WinForms-Fassung maß 1084 × 680;
        /// die Trägerkarte steht jetzt untereinander statt in zwei Reitern.</summary>
        public const int FENSTER_HOEHE = 840;

        private readonly int _projektId;
        private bool Katalogkontext { get { return _projektId <= 0; } }

        private List<EnergyCarrier> _traeger = new List<EnergyCarrier>();
        private EnergyCarrier _gewaehlt;

        /// <summary>ET‑6: Name des Trägers, den das letzte „Entfernen" nach sich gezogen hat.</summary>
        private string _stromZugeordnet = "";

        /// <summary>
        /// ET‑3 (08.09.2026): je Träger seine VERWENDUNG im Projekt (wer ihn beiträgt, ob er
        /// zugeordnet ist) — die Liste zeigt sie als Kurztext und markiert Unzugeordnete.
        /// Leer im Katalogkontext.
        /// </summary>
        private readonly Dictionary<int, ProjektEnergietraegerCtrl.Verwendung> _verwendung =
            new Dictionary<int, ProjektEnergietraegerCtrl.Verwendung>();

        // ---- Stand der offenen Trägerkarte ---------------------------------

        private EnergietraegerStand _stand;
        private List<EnergyConversion> _umrechnungen = new List<EnergyConversion>();

        /// <summary>
        /// Die Klappliste „Preisbasis" — je Einheit ein Eintrag, Abrechnungseinheit
        /// zuerst (Befund W4-B-1). <c>PreisbasisId</c> ist der Index HIER HINEIN,
        /// nicht mehr in <see cref="_umrechnungen"/>: Sonst wäre jede Bereinigung
        /// der Liste eine Verschiebung der gewählten Zeile.
        /// </summary>
        private List<EnergietraegerPreisCtrl.Preisbasis> _preisbasen
            = new List<EnergietraegerPreisCtrl.Preisbasis>();
        private List<UmrechnungsRegel> _regeln = new List<UmrechnungsRegel>();
        private StrompreisZerlegungModel _zerlegungModell;
        private BrennstoffBestandteilModel _bestandteilModell;

        /// <summary>
        /// Die BASISWERTE — so, wie sie in der Datenbank stehen: Arbeitspreis,
        /// Heiz- und Brennwert je ABRECHNUNGSEINHEIT, Leistungspreis in
        /// €/(kW·a) bzw. €/(kW·Monat), Grundpreis in €/a. Von ihnen weicht in
        /// der Anzeige allein der Arbeitspreis ab, und nur um den Faktor der
        /// gewählten Preisbasis (B1).
        /// </summary>
        private double _baseHi, _baseHs, _baseWork, _basePower, _baseGround;

        /// <summary>Der unberührte DB-Zustand für den Historienvergleich.</summary>
        private double _dbHi, _dbHs, _dbWork, _dbPower, _dbGround, _dbCO2, _dbSO2, _dbNOx;

        private EmissionenCtrl _emissionen;
        private int _katalogJahr;
        private string _unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;
        private double _co2PreisProjekt;
        private int _idBrennstoff;
        private string _abrechnungseinheit = "";

        // ---- Komponentenkontext (Auftrag 268) ------------------------------

        /// <summary>
        /// Die Komponente, aus der heraus die Verwaltung geöffnet wurde
        /// (<c>DbWerte.ERZEUGER_*</c>); leer = ohne Komponentenkontext.
        /// </summary>
        private string _erzeugerart = "";

        /// <summary>Gerätezeile des Brenners (<c>Tab_Heizkessel.ID</c> / <c>Tab_BHKW.ID</c>); 0 = keine.</summary>
        private int _geraeteId;

        /// <summary>
        /// Die zulässigen Gruppen (<see cref="EnergietraegerZulaessigkeit.ZulaessigeGruppen"/>);
        /// <c>null</c> = keine Einengung. Eine LEERE Menge (Solarthermie, Pufferspeicher)
        /// kommt hier nie an — siehe <see cref="KontextSetzen"/>.
        /// </summary>
        private IReadOnlyList<string> _zulaessigeGruppen;

        /// <summary>true, wenn die Komponente gar keinen Energieträger bezieht (nur Hinweis).</summary>
        private bool _ohneTraeger;

        /// <summary>
        /// ET‑E‑3 (Anwenderentscheid 17.09.2026): die zulässigen Gruppen ALLER Anlagen des
        /// Projekts (<see cref="EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt"/>);
        /// <c>null</c> = keine Einengung. Sie gilt NUR ohne Komponentenkontext und NUR für die
        /// Übernahme aus dem Katalog — die linke Liste bleibt vollständig, denn sie führt die
        /// Träger, die dem PROJEKT zugeordnet sind; einen davon zu verstecken hieße, eine
        /// vorhandene Zuordnung zu verschweigen.
        /// </summary>
        private IReadOnlyList<string> _projektGruppen;

        /// <summary>Der vorgewählte Träger der Komponente — er bleibt in der Liste, auch wenn er nicht passt.</summary>
        private int _vorwahl;

        // ---- Fehlende und geliehene Werte ------------------------------------
        //
        // Was die LESEKETTE zu den drei Größen sagt, gelesen beim Trägerwechsel.
        // Was seither im FELD steht, ist die zweite Hälfte der Aussage; zusammen
        // beantworten beide die Frage, die die Karte stellt: „Bliebe hier eine
        // Lücke, wenn ich jetzt speicherte?" Die Kette selbst wird NICHT
        // nachgebaut — sie steht in Emissionsquelle bzw. KostenEmissionRechner und
        // wird über EnergietraegerRueckfall befragt.
        private bool _co2AusKette;
        private bool _arbeitspreisAusKette;
        private bool _leistungspreisAusKette;

        /// <summary>Je Größe die Herleitung eines in dieser Sitzung geliehenen Wertes.</summary>
        private readonly Dictionary<string, string> _leihzeilen =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <param name="projektId">0 = Katalogkontext (Stammdaten).</param>
        public EnergietraegerHuelle(int projektId)
        {
            _projektId = projektId > 0 ? projektId : 0;
        }

        // =====================================================================
        // Einstieg
        // =====================================================================

        /// <summary>Der Fenstertitel — die Plattformhülle beschriftet damit ihr Fenster.</summary>
        public static string Titel()
        {
            return T("KDLG_ET_TITEL", "Energieträgerverwaltung");
        }

        /// <summary>
        /// Der PARAMETERSATZ der Energieträgerverwaltung (iU9-W5.4). Seit die
        /// Kostenseite selbst eine Razor-Komponente ist, erscheint sie in einer
        /// <c>Ueberlagerung</c> darin — dasselbe Fenster, dieselbe WebView
        /// (Risiko R2). <c>Geschlossen</c> setzt der Wirt.
        ///
        /// <para>Die Hüllen-INSTANZ hält den Bearbeitungsstand; sie lebt über
        /// die Rückrufe des Satzes so lange wie der Bereich.</para>
        /// </summary>
        /// <param name="traegerId">Vorwahl (KD6 § 9: „Energiekosten…" springt
        /// direkt auf den Träger der Komponente); 0 = der erste.</param>
        /// <param name="erzeugerart">Komponente, aus der heraus geöffnet wurde
        /// (<c>DbWerte.ERZEUGER_*</c> bzw. <c>KOSTEN_KOMPONENTE_PUFFERSPEICHER</c>);
        /// <c>null</c>/leer = ohne Komponentenkontext, dann ändert sich nichts
        /// (Menü Administration, Knopf auf der Kostenseite).</param>
        /// <param name="geraeteId">Gerätezeile des Brenners (<c>Tab_Heizkessel.ID</c>
        /// bzw. <c>Tab_BHKW.ID</c>); 0 = unbekannt.</param>
        public IReadOnlyDictionary<string, object> Gaben(int traegerId = 0,
                                                         string erzeugerart = null,
                                                         int geraeteId = 0)
        {
            KontextSetzen(traegerId, erzeugerart, geraeteId);
            ListeLaden();
            _katalogJahr = KatalogjahrErmitteln(_projektId, out _unternehmensart, out _co2PreisProjekt);

            return new Dictionary<string, object>
            {
                ["Liste"] = Listeneintraege(),
                // ET-5 (Anwenderbefund 16.09.2026): Die Liste war ein EINGEFRORENER Wert -
                // der Gabensatz lebt so lange wie der Dialog, also stand nach "Entfernen",
                // "Loeschen", "Neu...", "Variante", der Uebernahme und dem Stamm-Speichern
                // immer noch der Stand des Oeffnens da. Derselbe Rueckweg wie bei
                // FreieLaden (ET-1) und KostenfaktorKatalogHuelle.NeuLaden: KEIN zweites
                // ListeLaden() - die Schreibwege unten rufen es selbst, und ein weiteres
                // zoege StromTraegerSicherstellen ein zweites Mal.
                ["ListeNeuLaden"] =
                    new Func<IReadOnlyList<EnergietraegerDialog.EnergietraegerListe>>(Listeneintraege),
                ["TraegerVorwahl"] = traegerId > 0 ? (int?)traegerId : null,
                ["Katalogkontext"] = Katalogkontext,
                ["Gruppen"] = Gruppen(),

                ["TraegerLaden"] = new Func<int, EnergietraegerAnsicht>(TraegerLaden),
                ["Nachrechnen"] = new Func<EnergietraegerAnsicht>(Ansicht),
                ["PreisbasisGewechselt"] = EventCallback.Factory.Create<int>(new object(), PreisbasisWechseln),
                // Der Modus ist Katalogsache je Träger und wird SOFORT geschrieben
                // (KD4 § 7.1) — ohne gewählten Träger gibt es nichts zu schreiben.
                ["LeistungsModusGewechselt"] = EventCallback.Factory.Create<bool>(new object(),
                    monat =>
                    {
                        if (_gewaehlt == null) return;
                        EnergietraegerPreisCtrl.LeistungsModusSchreiben(_gewaehlt.ID, monat);
                        if (_stand != null) _stand.ReihenStatus = ReihenStatus();
                    }),
                ["RegelNeu"] = EventCallback.Factory.Create(new object(), (Action)RegelNeu),
                ["RegelAbschalten"] = new Func<UmrechnungsregelZeile, bool, bool>(DarfAbschalten),
                ["InArbeitspreis"] = EventCallback.Factory.Create(new object(), (Action)ArbeitspreisAusBestandteilen),
                ["KatalogUebernehmen"] = EventCallback.Factory.Create(new object(),
                    (Action)KatalogwerteUebernehmen),
                ["Speichern"] = new Func<bool>(Speichern),
                ["SpeichernGrund"] = new Func<string>(SpeichernGrund),
                ["SpeichernHinweis"] = new Func<string>(SpeichernHinweis),
                // Der Übernahmeweg aus der Kategorie: erst fragen, dann schreiben.
                // Die Wahl liefert die Kandidaten samt Wert und Einheit, die
                // Übernahme schreibt GENAU den bestätigten Wert in die
                // Projektübersteuerung (EnergietraegerRueckfall).
                ["LueckenWahl"] = new Func<string, Uebernahmewahl>(Kandidatenwahl),
                ["LueckeUebernehmen"] = new Func<string, int, bool>(LueckeUebernehmen),
                ["HistorieLoeschen"] = new Func<PreishistorieZeile, bool>(HistorieLoeschen),
                ["HistorieLoeschenGrund"] = new Func<string>(HistorieLoeschenGrund),
                ["UnterdialogGeschlossen"] = EventCallback.Factory.Create(new object(),
                    (Action)UnterdialogGeschlossen),

                ["StammSchreiben"] = new Func<string, int?, bool>(StammSchreiben),
                ["NamensGaben"] = new Func<IReadOnlyDictionary<string, object>>(NamensGaben),
                ["TraegerNeu"] = new Func<string, int>(TraegerNeu),
                ["TraegerVariante"] = new Func<int>(TraegerVariante),
                ["TraegerLoeschen"] = new Func<ValueTuple<bool, string>>(TraegerLoeschen),
                ["InsProjekt"] = new Func<IReadOnlyList<int>, int>(InsProjekt),
                ["AusProjekt"] = new Func<ValueTuple<bool, string>>(AusProjekt),
                // ET-6: Der Name des Stromtraegers, den ListeLaden nach dem Entfernen
                // WIEDER zugeordnet hat - leer, wenn nichts nachgezogen wurde.
                ["StromZugeordnet"] = new Func<string>(StromZugeordnet),
                // ET-1 (Anwenderbefund 08.09.2026, "Die Energietraegerverwaltung funktioniert
                // nicht"): Die freien Katalogtraeger kamen NIE an - "Aus Katalog uebernehmen..."
                // meldete immer "alle bereits zugeordnet". FreieLaden liest sie bei jedem
                // Oeffnen der Uebernahme frisch (nach einer Uebernahme ist die Menge kleiner).
                ["Freie"] = Freie(),
                ["FreieLaden"] = new Func<IReadOnlyList<ValueTuple<int, string>>>(Freie),
                ["NichtZugeordnetText"] = T("KDLG_ET_NICHT_ZUGEORDNET", "nicht zugeordnet"),

                ["KostenprofilGaben"] = new Func<IReadOnlyDictionary<string, object>>(KostenprofilGaben),
                ["SpotpreisGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => SpotpreisImportHuelle.Gaben(_projektId)),
                ["SaisonGaben"] = new Func<IReadOnlyDictionary<string, object>>(SaisonGaben),
                ["EmissionskatalogGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    EmissionskatalogGaben),
                ["EmissionskatalogAuswerten"] = new Action<EmissionskatalogErgebnis>(
                    EmissionskatalogAuswerten),

                ["KarteTexte"] = KarteTexte(),
                ["ZerlegungTexte"] = ZerlegungTexte(),
                ["BestandteilTexte"] = BestandteilTexte(),

                ["TitelText"] = T("KDLG_ET_TITEL", "Energieträgerverwaltung"),
                // ET-D: Der Dialogtitel nennt den Träger, an dem gerade gearbeitet
                // wird - „Energieträgerverwaltung" sagt nur, wo man ist.
                ["VorlageTitelTraeger"] = T("ETV_TITEL_TRAEGER", "Energieträger — {0}"),
                ["KontextText"] = KontextText(),
                ["PasstNichtText"] = T("KDLG_ET_PASST_NICHT", "passt nicht zur Komponente"),
                ["ListenTitel"] = T("KDLG_ET_LISTE", "Energieträger"),
                // Suche und Filter der Traegerliste (Anwenderwunsch 04.09.2026):
                // WORTGLEICH mit den Importdialogen - dieselbe Beschriftung,
                // damit die Auswahl dort und hier gleich heisst.
                ["SucheText"] = T("IMP_KAT_FILTER_SUCHE", "Filter:"),
                ["SucheLeerText"] = T("ETV_SUCHE_LEER", "Kein Treffer."),
                ["LeerText"] = T("ETV_LEER", "Bitte einen Energieträger wählen."),
                ["NeuText"] = T("KDLG_ET_BTN_NEU", "Neu…"),
                ["VarianteText"] = T("KDLG_ET_BTN_VARIANTE", "Variante"),
                ["LoeschenText"] = T("KDLG_ET_BTN_LOESCHEN", "Löschen"),
                ["UebernehmenText"] = T("KDLG_ET_BTN_UEBERNEHMEN", "Aus Katalog übernehmen…"),
                ["UebernehmenKurzText"] = T("KDLG_ET_UEBERNAHME_OK", "Übernehmen"),
                ["EntfernenText"] = T("KDLG_ET_BTN_ENTFERNEN", "Entfernen"),
                ["LabelStammName"] = T("KDLG_ET_STAMM_NAME", "Bezeichnung:"),
                ["LabelStammGruppe"] = T("KDLG_ET_STAMM_GRUPPE", "Gruppe:"),
                // DL-2 (Nr. 7, DL-Q5): Der Knopf „Bezeichnung speichern" ist entfallen;
                // Bezeichnung und Gruppe schreibt der Speichern- und OK-Weg mit.
                ["KarteProfilTitel"] = T("KPROF_KARTE_PROFIL_TITEL", "Kostenprofil"),
                ["KarteProfilInfo"] = T("KPROF_KARTE_PROFIL_INFO",
                    "Monatliche Preisniveaus des Strombezugs pflegen."),
                ["KarteSpotTitel"] = T("KPROF_KARTE_SPOT_TITEL", "Spotmarktpreise"),
                ["KarteSpotInfo"] = T("KPROF_KARTE_SPOT_INFO",
                    "Stundenpreise importieren und verwalten."),
                ["NeuTitel"] = T("KDLG_ET_NEU_TITEL", "Neuer Energieträger"),
                ["UebernahmeTitel"] = T("KDLG_ET_UEBERNAHME_TITEL", "Aus Katalog übernehmen"),
                ["UebernahmeFrage"] = T("ETV_UEBERNAHME_FRAGE",
                    "Welche Katalogträger sollen ins Projekt?"),
                ["UebernahmeLeer"] = T("KDLG_ET_UEBERNAHME_LEER",
                    "Alle Katalogträger sind dem Projekt bereits zugeordnet."),
                ["VorlageLoeschen"] = T("KDLG_ET_LOESCHEN_FRAGE", "Energieträger „{0}\" löschen?"),
                ["VorlageEntfernen"] = T("KDLG_ET_ENTFERNEN_FRAGE",
                    "Träger „{0}\" aus dem Projekt entfernen? (Der Katalogeintrag bleibt.)"),
                ["VorlageGesperrt"] = T("KDLG_ET_LOESCHEN_GESPERRT",
                    "Der Träger wird verwendet und bleibt erhalten: {0}"),
                ["VorlageStromZugeordnet"] = T("KDLG_ET_STROM_ZUGEORDNET",
                    "Das Projekt führt elektrische Anlagen; der Stromträger „{0}\" wurde zugeordnet."),
                ["MeldungStammLeer"] = T("KDLG_ET_STAMM_FEHLER", "Bezeichnung darf nicht leer sein."),
                ["TitelKostenprofil"] = MyResource.Resource.PREIS_PROFIL_TITEL,
                ["TitelSpotpreis"] = MyResource.Resource.PREIS_IMPORT_TITEL,
                ["TitelSaison"] = T("KDLG_LPR_TITEL", "Saisonale Leistungspreise"),
                ["TitelEmissionskatalog"] = T("EMK_TITEL", "Emissionsfaktor-Katalog"),
                // W-E2 (Mockup-Prüfung 04): OK, Abbrechen und Speichern sind Hausknöpfe —
                // ein eigener, gleichlautender Schlüssel je Dialog wäre eine zweite
                // Wahrheit über denselben Text. Die alten Schlüssel bleiben in der .resx
                // stehen (u. a. Prüfmuster der Formularkarte).
                ["SpeichernText"] = T("ADM_BTN_SPEICHERN", "Speichern"),
                ["AbbrechenText"] = T("ALLG_BTN_ABBRECHEN", "Abbrechen"),
                // DL-2 (DL-Q3 a): Die Aktionen der Listenspalte schreiben sofort -
                // der Kurztext sagt, was Abbrechen wirklich verwirft.
                ["AbbrechenKurztext"] = T("KDLG_ET_TIP_ABBRECHEN",
                    "Verwirft nur die ungespeicherten Eingaben; "
                    + "angelegte Träger bleiben."),
                ["OkText"] = T("ALLG_BTN_OK", "OK"),
                ["JaText"] = T("KKOMP_BTN_JA", "Ja"),
                ["NeinText"] = T("KKOMP_BTN_NEIN", "Nein"),
                ["VorlageGespeichert"] = " — " + T("KDLG_GESPEICHERT", "gespeichert {0:HH:mm} Uhr")
                    .Replace("{0:HH:mm}", "{0}")
            };
        }

        // =====================================================================
        // Komponentenkontext (Auftrag 268)
        // =====================================================================

        /// <summary>
        /// Nimmt den Komponentenkontext auf und fragt den Kern, was zulässig ist
        /// (<see cref="EnergietraegerZulaessigkeit"/> — die EINE Wahrheit; hier wird
        /// keine zweite Regel gerechnet).
        ///
        /// <para><b>Solarthermie und Pufferspeicher werden GENANNT, nicht gefiltert.</b>
        /// Sie beziehen keine Energie; eine leere Trägerliste wäre aber eine Sackgasse.
        /// Die Kopfzeile sagt deshalb „kein eigener Energieträger", und die Verwaltung
        /// bleibt vollständig — gepflegt werden die Träger des PROJEKTS, nicht die der
        /// Komponente.</para>
        /// </summary>
        private void KontextSetzen(int traegerId, string erzeugerart, int geraeteId)
        {
            _erzeugerart = (erzeugerart ?? "").Trim();
            _geraeteId = geraeteId > 0 ? geraeteId : 0;
            _vorwahl = traegerId > 0 ? traegerId : 0;

            IReadOnlyList<string> gruppen = null;
            try { gruppen = EnergietraegerZulaessigkeit.ZulaessigeGruppen(_erzeugerart, _geraeteId); }
            catch { gruppen = null; }

            _ohneTraeger = gruppen != null && gruppen.Count == 0;
            _zulaessigeGruppen = _ohneTraeger ? null : gruppen;

            // ET-E-3: Ohne Komponentenkontext (Menue Administration, Knopf auf der
            // Kostenseite ohne gewaehlte Anlagenzeile) engt das PROJEKT ein - auf das,
            // was seine Anlagen ueberhaupt beziehen koennen. Der Katalogkontext
            // (_projektId <= 0) bleibt frei, und mit Erzeugerart bleibt es beim
            // Einzelfall.
            _projektGruppen = null;
            if (_erzeugerart.Length == 0 && _projektId > 0)
            {
                try
                {
                    _projektGruppen =
                        EnergietraegerZulaessigkeit.ZulaessigeGruppenFuerProjekt(_projektId);
                }
                catch { _projektGruppen = null; }
            }
        }

        /// <summary>
        /// Die Gruppen, auf die die ÜBERNAHME aus dem Katalog eingeengt ist: die der
        /// Komponente, sonst die des Projekts. <c>null</c> = keine Einengung.
        /// </summary>
        private IReadOnlyList<string> UebernahmeGruppen
        { get { return _zulaessigeGruppen ?? _projektGruppen; } }

        /// <summary>Gilt eine Einengung? (Nur dann werden Liste und Übernahme gefiltert.)</summary>
        private bool Eingeengt { get { return _zulaessigeGruppen != null; } }

        /// <summary>Passt dieser Träger zur Komponente?</summary>
        private bool Passt(EnergyCarrier c)
        {
            return c != null && EnergietraegerZulaessigkeit.PasstGruppe(_zulaessigeGruppen, c.GroupCode);
        }

        /// <summary>
        /// Die Kopfzeile: Kontext (Katalog oder Projekt) und — wenn die Verwaltung aus
        /// einer Komponente heraus geöffnet wurde — wofür die Liste eingeengt ist.
        /// </summary>
        private string KontextText()
        {
            // ET-D: Die Kopfzeile nennt, WO man ist und WELCHE Preise gelten -
            // netto, ausnahmslos. Der Projektname steht da, wo bis ET-D die
            // Projektnummer stand: Eine Nummer sagt niemandem, in welchem Projekt
            // er gerade Preise pflegt.
            string kontext;
            if (Katalogkontext)
            {
                kontext = T("ETV_KONTEXT_KATALOG_NETTO", "Katalog · Preise netto");
            }
            else
            {
                string name = "";
                try { name = StartseiteCtrl.Projektname(_projektId) ?? ""; }
                catch { name = ""; }
                if (name.Trim().Length == 0)
                    name = string.Format(CultureInfo.CurrentCulture,
                        T("ETV_KONTEXT_PROJEKTNUMMER", "Projekt {0}"), _projektId);

                kontext = string.Format(CultureInfo.CurrentCulture,
                    T("ETV_KONTEXT_NETTO", "{0} · Preise netto"), name);
            }

            // ET-E-3: Ohne Komponentenkontext sagt die Kopfzeile, worauf die Uebernahme
            // eingeengt ist - die Anlagen des Projekts.
            if (_erzeugerart.Length == 0)
            {
                if (_projektGruppen == null) return kontext;
                return kontext + " — " + string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_ET_KONTEXT_PROJEKTANLAGEN",
                      "Übernahme eingeengt auf die Anlagen des Projekts: {0}"),
                    string.Join(", ", _projektGruppen));
            }

            string zusatz;
            if (_ohneTraeger)
                zusatz = string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_ET_KONTEXT_OHNE_TRAEGER", "für {0}: kein eigener Energieträger"),
                    _erzeugerart);
            else if (Eingeengt)
                zusatz = string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_ET_KONTEXT_KOMPONENTE", "für {0}: nur Gruppe {1}"),
                    _erzeugerart, string.Join(", ", _zulaessigeGruppen));
            else
                zusatz = string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_ET_KONTEXT_ALLE", "für {0}: alle Energieträger"), _erzeugerart);

            return kontext + " — " + zusatz;
        }

        // =====================================================================
        // Trägerliste (Ä13)
        // =====================================================================

        /// <summary>
        /// Die Träger unter ihren Gruppen — wortgleich aus
        /// <c>Form_Energietraeger.SetControls</c>: sortiert nach Gruppe, dann
        /// Name; Köpfe sind nicht wählbar.
        /// </summary>
        private void ListeLaden()
        {
            _verwendung.Clear();

            // ET-2 (08.09.2026): Die elektrische Welt bekommt ihren Stromtraeger, BEVOR die
            // Liste gelesen wird - das Projekt des Anwenderbefunds (1026: Waermepumpe, PV,
            // Speicher, kein Strom zugeordnet) zeigt danach "Strom > Elektrische Energie".
            if (_projektId > 0)
            {
                try { ProjektEnergietraegerCtrl.StromTraegerSicherstellen(_projektId); }
                catch { }
            }

            _traeger = KostenSummenCtrl.GetAllCarriers(_projektId);

            // ET-3: die VERWENDETEN Traeger des Projekts - ein verwendeter, aber nicht
            // zugeordneter Traeger (etwa nach "Entfernen" eines Brennertraegers) steht
            // markiert in der Liste, statt zu fehlen; Speichern ordnet ihn zu.
            if (_projektId > 0)
            {
                try
                {
                    List<EnergyCarrier> katalog = null;
                    foreach (ProjektEnergietraegerCtrl.Verwendung v in
                             ProjektEnergietraegerCtrl.Verwendete(_projektId))
                    {
                        _verwendung[v.CarrierId] = v;
                        if (v.Zugeordnet || _traeger.Exists(c => c.ID == v.CarrierId)) continue;
                        if (katalog == null) katalog = KostenSummenCtrl.GetAllCarriers(0);
                        EnergyCarrier frei = katalog.Find(c => c.ID == v.CarrierId);
                        if (frei != null) _traeger.Add(frei);
                    }
                }
                catch { _verwendung.Clear(); }
            }

            // Auftrag 268: Mit Komponentenkontext bleiben nur die zulaessigen Traeger
            // stehen - PLUS der bereits zugeordnete Traeger der Komponente, auch wenn er
            // nicht passt. Ihn wegzufiltern hiesse, eine falsche Zuordnung zu verstecken,
            // statt sie zu zeigen; die Liste markiert ihn stattdessen.
            if (Eingeengt)
            {
                var behalten = new List<EnergyCarrier>();
                foreach (EnergyCarrier c in _traeger)
                    if (Passt(c) || c.ID == _vorwahl) behalten.Add(c);
                _traeger = behalten;
            }

            _traeger.Sort((a, b) =>
            {
                int g = string.Compare(a.GroupCode ?? "", b.GroupCode ?? "",
                                       StringComparison.CurrentCultureIgnoreCase);
                return g != 0 ? g : string.Compare(a.Name, b.Name,
                                       StringComparison.CurrentCultureIgnoreCase);
            });
        }

        private IReadOnlyList<EnergietraegerDialog.EnergietraegerListe> Listeneintraege()
        {
            var liste = new List<EnergietraegerDialog.EnergietraegerListe>();
            string gruppe = null;
            foreach (EnergyCarrier c in _traeger)
            {
                string g = string.IsNullOrEmpty(c.GroupCode)
                    ? T("KDLG_ET_GRUPPE_SONSTIGE", "Sonstige") : c.GroupCode;
                if (!string.Equals(g, gruppe, StringComparison.CurrentCultureIgnoreCase))
                {
                    gruppe = g;
                    liste.Add(new EnergietraegerDialog.EnergietraegerListe(null, g));
                }
                liste.Add(new EnergietraegerDialog.EnergietraegerListe(
                    c.ID, c.Name, ListenKurztext(c.ID), ListenZugeordnet(c.ID), Passt(c)));
            }
            return liste;
        }

        /// <summary>ET‑3: „verwendet von: Wärmepumpe „CS6800iAW", Photovoltaik „Jinkosolar"" — leer, wenn unverwendet.</summary>
        private string ListenKurztext(int id)
        {
            ProjektEnergietraegerCtrl.Verwendung v;
            if (_projektId <= 0 || !_verwendung.TryGetValue(id, out v)) return "";
            string text = string.Format(CultureInfo.CurrentCulture,
                T("KDLG_ET_VERWENDET_VON", "verwendet von: {0}"), v.BeitraegerText);
            return v.Zugeordnet ? text : text + " — " + T("KDLG_ET_NICHT_ZUGEORDNET", "nicht zugeordnet");
        }

        /// <summary>ET‑3: ist der Träger dem Projekt zugeordnet? (Im Katalogkontext immer.)</summary>
        private bool ListenZugeordnet(int id)
        {
            ProjektEnergietraegerCtrl.Verwendung v;
            return _projektId <= 0 || !_verwendung.TryGetValue(id, out v) || v.Zugeordnet;
        }

        /// <summary>
        /// ET‑1: die Katalogträger, die dem Projekt noch nicht zugeordnet sind, mit Gruppe.
        /// Auftrag 268: Mit Komponentenkontext bietet „Aus Katalog übernehmen…" nur die
        /// zulässigen an — was die Liste nicht zeigt, soll auch die Übernahme nicht
        /// hereinholen. ET‑E‑3: OHNE Komponentenkontext gilt dieselbe Regel für das ganze
        /// Projekt — angeboten wird, was die Anlagen des Projekts beziehen können.
        /// </summary>
        private IReadOnlyList<ValueTuple<int, string>> Freie()
        {
            var liste = new List<ValueTuple<int, string>>();
            if (_projektId <= 0) return liste;
            try
            {
                IReadOnlyList<string> gruppen = UebernahmeGruppen;
                foreach (EnergyCarrier c in EnergietraegerKatalogCtrl.NichtZugeordnete(_projektId))
                {
                    if (gruppen != null
                     && !EnergietraegerZulaessigkeit.PasstGruppe(gruppen, c.GroupCode)) continue;
                    liste.Add(new ValueTuple<int, string>(c.ID,
                        (string.IsNullOrEmpty(c.GroupCode) ? "" : c.GroupCode + " › ") + c.Name));
                }
            }
            catch { }
            return liste;
        }

        private IReadOnlyList<ValueTuple<int, string>> Gruppen()
        {
            var liste = new List<ValueTuple<int, string>>();
            int n = 0;
            foreach (string g in EnergietraegerKatalogCtrl.Gruppen())
                liste.Add(new ValueTuple<int, string>(n++, g));
            return liste;
        }

        private string GruppenName(int? id)
        {
            int n = 0;
            foreach (string g in EnergietraegerKatalogCtrl.Gruppen())
            {
                if (id.HasValue && n == id.Value) return g;
                n++;
            }
            return "";
        }

        // =====================================================================
        // Trägerkarte laden (wortgleich aus ucFuelSettings.LoadData)
        // =====================================================================

        /// <summary>Lädt die Trägerkarte und gibt die fertige Ansicht zurück.</summary>
        private EnergietraegerAnsicht TraegerLaden(int id)
        {
            TraegerWaehlen(id);
            return Ansicht();
        }

        /// <summary>
        /// Was die Komponente zeigt: der Stand und alles, was hier daraus
        /// gerechnet wurde — Summenzeilen der beiden Preisblöcke, der
        /// Arbeitspreis in ct/kWh, die Schnellwahlsätze und die Kartenstatus.
        /// </summary>
        private EnergietraegerAnsicht Ansicht()
        {
            var a = new EnergietraegerAnsicht { Stand = _stand };
            if (_stand == null || _gewaehlt == null) return a;

            // B1 (Anwenderbefund 14.09.2026, stehen gebliebene Formelzeile):
            // Dies ist der Weg JEDER Feldaenderung - die Komponente meldet
            // „Geaendert", der Wirt ruft „Nachrechnen". Ohne das Nachziehen hier
            // blieben Basiswerte, Formel- und Effektivzeile auf dem Stand des
            // Ladens stehen, und gespeichert wurde der ALTE Wert.
            Nachziehen();

            a.ArbeitspreisCtKwh = ArbeitspreisCtKwh();
            a.StammName = _gewaehlt.Name ?? "";
            a.StammGruppe = GruppenIndex(_gewaehlt.GroupCode);

            if (_stand.Zerlegung != null && _zerlegungModell != null)
            {
                InStromModell(_stand.Zerlegung, _zerlegungModell);
                SpeicherEngine.Preiszerlegung satz =
                    StrompreisZerlegungCtrl.AlsPreiszerlegung(_zerlegungModell);

                // SP-E-2: Der Block ZERLEGT den Arbeitspreis. Ausgewiesen werden
                // deshalb die Summe der aktiven Anteile und — als Kohärenzzeile — ihr
                // Abstand zum Arbeitspreis der Karte, nicht mehr ein „wirksamer
                // Aufschlag". Denselben Weg geht der Brennstoffblock, Zeile für Zeile
                // aus derselben Stelle (Preisblock).
                a.ZerlegungAnzeige = Preisblock(
                    satz, a.ArbeitspreisCtKwh,
                    MyResource.Resource.PREIS_SUMME_AKTIV,
                    T("PREIS_REST", "Nicht aufgeschlüsselter Rest: {0} ct/kWh"));

                // Der Rest-Vorschlag für die Beschaffung: Arbeitspreis minus Summe der
                // ÜBRIGEN aktiven Anteile. Er wird angeboten, nicht geschrieben — und
                // nur, solange die Beschaffung leer ist und ein Arbeitspreis dasteht.
                double ohneBeschaffung =
                    satz.SummeAktivOhneCtKwh(StrompreisZerlegungCtrl.KOMP_BESCHAFFUNG);
                double vorschlag = a.ArbeitspreisCtKwh - ohneBeschaffung;
                a.BeschaffungVorschlag =
                    (_zerlegungModell.Beschaffung == 0.0 || !_zerlegungModell.Beschaffung_Aktiv)
                    && a.ArbeitspreisCtKwh > 0.0 && vorschlag > 0.0
                        ? (double?)vorschlag
                        : null;

                a.SatzRegelfall = StromsteuerSatz(DbWerte.GESETZ_STROMST_REGELSATZ,
                    StrompreisZerlegungModel.STROMSTEUER_REGELFALL,
                    !ReduzierterSatzEmpfohlen());
                a.SatzReduziert = StromsteuerSatz(DbWerte.GESETZ_STROMST_REDUZIERT,
                    StrompreisZerlegungModel.STROMSTEUER_REDUZIERT,
                    ReduzierterSatzEmpfohlen());
            }

            if (_stand.Bestandteile != null && _bestandteilModell != null)
            {
                InBrennstoffModell(_stand.Bestandteile, _bestandteilModell);

                // Kein Modus mehr (Anwenderentscheid 17.09.2026): Summe der aktiven
                // Bestandteile, Rest gegen den Arbeitspreis — dieselbe Rechnung wie
                // oben beim Strom, nur mit den Texten dieses Trägers.
                SpeicherEngine.Preiszerlegung bsatz =
                    BrennstoffBestandteilCtrl.AlsPreiszerlegung(_bestandteilModell);

                a.BestandteilAnzeige = MitKohaerenz(Preisblock(
                    bsatz, a.ArbeitspreisCtKwh,
                    T("BB_SUMME_AKTIV", "Summe der aktiven Bestandteile: {0} ct/kWh"),
                    T("BB_REST", "Nicht aufgeschlüsselter Rest: {0} ct/kWh")),
                    bsatz, a.ArbeitspreisCtKwh);

                // ET-D-1: Die Anzeigekante — Einheit, Heizwert, Herleitungen und der
                // Rest-Vorschlag. Gerechnet wird weiter in ct/kWh.
                a.BestandteilHeizwert = _gewaehlt.HasHi ? _baseHi : 0.0;
                a.BestandteilEinheit = EnergietraegerPreiskarte.AnteilEinheit(
                    _abrechnungseinheit, a.BestandteilHeizwert);
                a.BestandteilHinweis = a.BestandteilHeizwert > 0.0 ? "" : T("BB_HINWEIS_OHNE_HI",
                    "Ohne Heizwert stehen die Bestandteile in ct/kWh.");
                a.HerleitungEnergiesteuer = HerleitungEnergiesteuer();
                a.HerleitungCo2 = HerleitungCo2();

                double ohneVertrieb =
                    bsatz.SummeAktivOhneCtKwh(BrennstoffBestandteilCtrl.KOMP_VERTRIEB);
                double restVorschlag = a.ArbeitspreisCtKwh - ohneVertrieb;
                a.VertriebVorschlag =
                    (!_bestandteilModell.Vertrieb.HasValue || !_bestandteilModell.Vertrieb_Aktiv)
                    && a.ArbeitspreisCtKwh > 0.0 && restVorschlag > 0.0
                        ? (double?)restVorschlag
                        : null;

                a.SatzRegel = EnergiesteuerSatz(
                    WirtschaftlichkeitCtrl.EnergiesteuerSchluessel(_idBrennstoff, false),
                    T("BB_BTN_SATZ_REGEL", "§ 2: {0}"));
                a.Satz53a = EnergiesteuerSatz(
                    WirtschaftlichkeitCtrl.EnergiesteuerSchluessel(_idBrennstoff, true),
                    T("BB_BTN_SATZ_53A", "§ 53a: {0}"));
                a.Satz54 = EnergiesteuerSatz(
                    WirtschaftlichkeitCtrl.Energiesteuer54Schluessel(_idBrennstoff),
                    T("BB_BTN_SATZ_54", "§ 54: {0}"));
                a.SatzCo2 = Co2Satz();
            }

            bool strom = string.Equals(_gewaehlt.PricingModel, "ELECTRICITY",
                                       StringComparison.OrdinalIgnoreCase);
            a.MitStromkarten = strom;
            a.MitKostenprofil = strom && _projektId > 0;
            if (strom) KartenStatus(a);

            return a;
        }

        private int? GruppenIndex(string gruppe)
        {
            int n = 0;
            foreach (string g in EnergietraegerKatalogCtrl.Gruppen())
            {
                if (string.Equals(g, gruppe ?? "", StringComparison.Ordinal)) return n;
                n++;
            }
            return null;
        }

        /// <summary>
        /// Die Summen- und Restzeile EINES Preisblocks — die eine Stelle, aus der
        /// beide Träger ihre Zahlen beziehen.
        /// </summary>
        /// <remarks>
        /// Gerechnet wird in <see cref="Preisanteile"/> über den Engine-Satz; hier
        /// entstehen nur die beiden Texte. Ein NEGATIVER Rest heisst: Die
        /// ausgewiesenen Anteile sind zusammen teurer als der Preis — das wird als
        /// Warnfarbe benannt, nicht geglättet.
        /// </remarks>
        /// <param name="satz">Der Engine-Satz des Trägers.</param>
        /// <param name="arbeitspreisCtKwh">Der Arbeitspreis der Trägerkarte [ct/kWh].</param>
        /// <param name="vorlageSumme">Textvorlage der Summenzeile, ein Platzhalter.</param>
        /// <param name="vorlageRest">Textvorlage der Restzeile, ein Platzhalter.</param>
        private static PreisblockAnzeige Preisblock(SpeicherEngine.Preiszerlegung satz,
                                                    double arbeitspreisCtKwh,
                                                    string vorlageSumme, string vorlageRest)
        {
            double summe = Preisanteile.SummeCtKwh(satz);
            double rest = Preisanteile.RestCtKwh(arbeitspreisCtKwh, satz);

            return new PreisblockAnzeige(string.Format(vorlageSumme, Anzeige(summe)),
                                         string.Format(vorlageRest, Anzeige(rest)),
                                         rest < 0.0);
        }

        /// <summary>
        /// Die Toleranz der Kohärenzprüfung: 0,0001 €/kWh — in ct/kWh sind das
        /// 0,01. Darunter ist ein Unterschied Rundung, darüber eine Aussage.
        /// </summary>
        private const double KOHAERENZ_TOLERANZ_CT_KWH = 0.01;

        /// <summary>
        /// Die EINE Summenzeile des Bestandteilblocks (ET-D-1): Summe der aktiven
        /// Anteile in der ANZEIGEEINHEIT und die Aussage, ob sie zum Arbeitspreis
        /// passt. Bis ET-D stand die Kohärenzzeile ausnahmslos auf „✓" — sie
        /// zeigte den Arbeitspreis, sie prüfte ihn nicht.
        /// </summary>
        private PreisblockAnzeige MitKohaerenz(PreisblockAnzeige anzeige,
                                               SpeicherEngine.Preiszerlegung satz,
                                               double arbeitspreisCtKwh)
        {
            CultureInfo k = CultureInfo.CurrentCulture;
            double summe = Preisanteile.SummeCtKwh(satz);
            double abstand = summe - arbeitspreisCtKwh;
            bool abweichend = Math.Abs(abstand) > KOHAERENZ_TOLERANZ_CT_KWH;

            string einheit = EnergietraegerPreiskarte.AnteilEinheit(_abrechnungseinheit, _baseHi);
            string summeText = AnteilText(summe);

            string text = abweichend
                ? string.Format(k, T("BB_KOHAERENZ_AB",
                        "Summe der Bestandteile {0} {1} — weicht um {2} {1} ab"),
                    summeText, einheit, AnteilText(Math.Abs(abstand)))
                : string.Format(k, T("BB_KOHAERENZ_OK",
                        "Summe der Bestandteile {0} {1} — deckungsgleich mit dem Arbeitspreis"),
                    summeText, einheit);

            return anzeige with { KohaerenzText = text, Abweichend = abweichend };
        }

        /// <summary>Ein Anteil [ct/kWh] als Text in der Anzeigeeinheit.</summary>
        private string AnteilText(double ctKwh)
        {
            return EnergietraegerPreiskarte.AnteilJeEinheit(ctKwh, _baseHi)
                .ToString("0.####", CultureInfo.CurrentCulture);
        }

        private static string Anzeige(double wert)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture);
        }

        private void TraegerWaehlen(int id)
        {
            _gewaehlt = null;
            foreach (EnergyCarrier c in _traeger)
                if (c.ID == id) { _gewaehlt = c; break; }
            if (_gewaehlt == null) { _stand = null; return; }

            _umrechnungen = EnergietraegerPreisCtrl.Umrechnungen(_gewaehlt.ID_Brennstoff);
            _idBrennstoff = _gewaehlt.ID_Brennstoff;
            _abrechnungseinheit = _gewaehlt.BillingUnit ?? "";

            var stand = new EnergietraegerStand
            {
                TraegerZeile = _gewaehlt.Name + "  (VDI 3805 " + _gewaehlt.Code + ")",
                GruppeZeile = "Gruppe: " + _gewaehlt.GroupCode,
                MitHeizwert = _gewaehlt.HasHi,
                MitBrennwert = _gewaehlt.HasHs,
                MitLeistungspreis = _gewaehlt.HasPowerPrice,
                MitFormel = _gewaehlt.HasHi,
                Basiseinheit = _gewaehlt.BillingUnit ?? "",
                GueltigAb = DateOnly.FromDateTime(DateTime.Now),
                EinheitGrundpreis = "€/a",
                // B3: Katalogwerte holen kann nur, wer ein Projekt pflegt - im
                // Katalog SIND die Felder die Katalogwerte.
                MitKatalogUebernahme = _projektId > 0,
                // Q11 (Weg 2 aus Nach #291): Die Leistungspreis-Staffel pflegt der
                // STROMtraeger im PROJEKT - sie steht an der Projektuebersteuerung,
                // der Katalog fuehrt keine.
                MitStaffel = _projektId > 0 &&
                             string.Equals(_gewaehlt.PricingModel, "ELECTRICITY",
                                           StringComparison.OrdinalIgnoreCase)
            };

            EnergietraegerPreisCtrl.Projektpreis projekt =
                EnergietraegerPreisCtrl.ProjektpreisLesen(_projektId, _gewaehlt.ID);

            // Die Einheit, die die Projektzeile als Preisbasis gemerkt hat.
            string gemerkteBasis;

            if (projekt != null)
            {
                // Ä-BK3: Die drei Preisspalten dürfen NULL sein — Rückfall ist
                // derselbe Katalogwert, den auch der else-Zweig setzt.
                stand.Arbeitspreis = projekt.Arbeitspreis ?? _gewaehlt.price_work;
                stand.Grundpreis = projekt.Grundpreis ?? _gewaehlt.price_base;
                stand.Leistungspreis = projekt.Leistungspreis ?? _gewaehlt.price_power;
                stand.Heizwert = projekt.Hi ?? _gewaehlt.HiKwhPerUnit;
                stand.Brennwert = projekt.Hs ?? _gewaehlt.HsKwhPerUnit;
                stand.AltCO2 = projekt.CO2 ?? _gewaehlt.CO2;
                stand.AltSO2 = projekt.SO2 ?? _gewaehlt.SO2;
                stand.AltNOx = projekt.NOx ?? _gewaehlt.NOx;

                // ETAPPE E7c (Schritt F, Schemaschritt 108, Mockup U32): Die Preisbasis
                // ist ein EIGENER Kartenzustand und kommt aus ihrer Spalte - nicht mehr
                // aus der Regelkennung ID_Umrechnung, die ohne Regel nach kWh auf -1
                // fiel und die Wahl „kWh" beim naechsten Oeffnen still verlor. Leer heisst
                // Abrechnungseinheit (die Vorgabe einer neu zugeordneten Zeile); fehlt
                // die Spalte (Datenbank vor 108), sagt es die Herleitungszeile.
                gemerkteBasis = string.IsNullOrWhiteSpace(projekt.Preisbasis)
                    ? null : projekt.Preisbasis.Trim();
                if (projekt.PreisbasisSpalteFehlt)
                    stand.PreisbasisHerleitung = T("ETV_PREISBASIS_OHNE_SPALTE",
                        "Die gewählte Preisbasis kann diese Datenbank noch nicht speichern " +
                        "(Schemastand vor 108) — die Karte zeigt die Abrechnungseinheit.");

                // Q11: die zweistufige Leistungspreis-Staffel (Schemaschritt 104) —
                // leer bleibt leer, sie hat keinen Katalogwert.
                stand.StaffelGrenze = projekt.Staffel.GrenzeKW;
                stand.StaffelPreis1 = projekt.Staffel.Preis1EurKWa;
                stand.StaffelPreis2 = projekt.Staffel.Preis2EurKWa;
            }
            else
            {
                stand.Arbeitspreis = _gewaehlt.price_work;
                stand.Grundpreis = _gewaehlt.price_base;
                stand.Leistungspreis = _gewaehlt.price_power;
                stand.Heizwert = _gewaehlt.HiKwhPerUnit;
                stand.Brennwert = _gewaehlt.HsKwhPerUnit;
                stand.AltCO2 = _gewaehlt.CO2;
                stand.AltSO2 = _gewaehlt.SO2;
                stand.AltNOx = _gewaehlt.NOx;

                gemerkteBasis = _gewaehlt.BillingUnit;
            }

            // UR-1: Die Preisbasis „kWh" traegt den HEIZWERT als Faktor, nicht den
            // Faktor einer Umrechnungsregel. Die Liste kann deshalb erst stehen,
            // wenn der Heizwert gelesen ist.
            _preisbasen = EnergietraegerPreisCtrl.Preisbasen(
                _gewaehlt.BillingUnit, _umrechnungen, stand.Heizwert);

            var basen = new List<ValueTuple<int, string>>();
            for (int i = 0; i < _preisbasen.Count; i++)
                basen.Add(new ValueTuple<int, string>(i, _preisbasen[i].Einheit));
            stand.Preisbasen = basen;
            stand.PreisbasisId = IndexZuEinheit(gemerkteBasis);

            // DIE BASISWERTE SIND, WAS IN DER DATENBANK STEHT - je
            // ABRECHNUNGSEINHEIT. Heiz- und Brennwert bleiben es auch in der
            // Anzeige (Stoffwerte); nur der Arbeitspreis folgt der Preisbasis.
            _baseHi = stand.Heizwert;
            _baseHs = stand.Brennwert;
            _baseWork = stand.Arbeitspreis;
            _basePower = stand.Leistungspreis;
            _baseGround = stand.Grundpreis;

            _dbWork = _baseWork; _dbGround = _baseGround; _dbPower = _basePower;
            _dbHi = _baseHi; _dbHs = _baseHs;
            _dbCO2 = stand.AltCO2; _dbSO2 = stand.AltSO2; _dbNOx = stand.AltNOx;

            _stand = stand;
            AnzeigeAusBasis();

            stand.LeistungsModusMonat = string.Equals(
                EnergietraegerPreisCtrl.LeistungsModus(_gewaehlt.ID),
                DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal);

            _regeln = EnergieEinheitenPruefung.RegelnDesBrennstoffs(_gewaehlt.ID_Brennstoff);
            stand.ReihenStatus = ReihenStatus();

            // Ein Trägerwechsel verwirft jede geliehene Zeile: Sie gehört dem
            // Träger, nicht der Karte.
            _leihzeilen.Clear();
            KetteLesen();

            BloeckeAufbauen();
            EmissionenAufbauen();
            HistorieLaden();
            Nachziehen();
        }

        /// <summary>
        /// Die ANZEIGEWERTE aus den Basiswerten (B1): Heizwert, Brennwert,
        /// Leistungspreis und Grundpreis stehen unverändert da — allein der
        /// Arbeitspreis wird in die gewählte Preisbasis gerechnet.
        /// </summary>
        private void AnzeigeAusBasis()
        {
            if (_stand == null) return;

            _stand.Heizwert = _baseHi;
            _stand.Brennwert = _baseHs;
            _stand.Leistungspreis = _basePower;
            _stand.Grundpreis = _baseGround;
            _stand.Arbeitspreis = EnergietraegerPreiskarte.AnzeigeArbeitspreis(_baseWork, Faktor());
        }

        /// <summary>
        /// Der Faktor der gewählten Preisbasis — Abrechnungseinheit → Preisbasis.
        ///
        /// <para><b>UR-1.</b> Es gibt genau zwei Basen: die Abrechnungseinheit
        /// (Faktor 1) und die Kilowattstunde. Deren Faktor ist der HEIZWERT, und
        /// zwar der, der gerade im Feld steht — wer Hi ändert, ändert damit die
        /// Umrechnung des Arbeitspreises. Bis ET-D stand hier der
        /// <c>factor</c> einer Umrechnungsregel; in der Testdatenbank ist das
        /// 0,5 gegen Hi 10,5.</para>
        /// </summary>
        private double Faktor()
        {
            if (_gewaehlt == null || !_gewaehlt.HasHi) return 1.0;
            EnergietraegerPreisCtrl.Preisbasis b = AktuelleBasis();
            if (b == null) return 1.0;

            if (EnergietraegerPreiskarte.IstKwh(b.Einheit)
                && !EnergietraegerPreiskarte.IstKwh(_abrechnungseinheit))
            {
                double hi = _stand != null ? _stand.Heizwert : _baseHi;
                return hi > 0.0 ? hi : 1.0;
            }

            return 1.0;
        }

        private int? IndexZuEinheit(string einheit)
        {
            return EnergietraegerPreisCtrl.PreisbasisIndex(_preisbasen, einheit);
        }

        /// <summary>Die gewählte Preisbasis; <c>null</c> = keine Liste.</summary>
        private EnergietraegerPreisCtrl.Preisbasis AktuelleBasis()
        {
            int? i = _stand != null ? _stand.PreisbasisId : null;
            return i.HasValue && i.Value >= 0 && i.Value < _preisbasen.Count
                ? _preisbasen[i.Value] : null;
        }

        private EnergyConversion AktuelleUmrechnung()
        {
            EnergietraegerPreisCtrl.Preisbasis b = AktuelleBasis();
            return b != null ? b.Umrechnung : null;
        }

        private string AktuelleEinheit()
        {
            EnergietraegerPreisCtrl.Preisbasis b = AktuelleBasis();
            return b != null && !string.IsNullOrEmpty(b.Einheit)
                ? b.Einheit : (_gewaehlt != null ? _gewaehlt.BillingUnit ?? "" : "");
        }

        // =====================================================================
        // Die beiden Preisblöcke (AP4 / B2)
        // =====================================================================

        private void BloeckeAufbauen()
        {
            _zerlegungModell = null;
            _bestandteilModell = null;
            if (_gewaehlt == null || _stand == null) return;

            string modell = (_gewaehlt.PricingModel ?? "").ToUpperInvariant();

            if (string.Equals(modell, StrompreisZerlegungCtrl.PRICING_MODEL_STROM,
                              StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    StrompreisZerlegungCtrl.StelleSpaltenSicher();
                    _zerlegungModell = new StrompreisZerlegungCtrl().Read(_projektId, _gewaehlt.ID);
                    _stand.Zerlegung = AusStromModell(_zerlegungModell);
                }
                catch (Exception ex)
                {
                    // Fehlende Strompreis-Details dürfen die Preispflege nicht
                    // blockieren — etwa ohne Migrationsschritt 12 oder 83.
                    Console.WriteLine("Die Strompreis-Details konnten nicht aufgebaut werden: " + ex.Message);
                    _zerlegungModell = null;
                }
                return;
            }

            if (Array.IndexOf(PREISMODELLE_BRENNSTOFF, modell) < 0) return;

            try
            {
                BrennstoffBestandteilCtrl.StelleSpaltenSicher();
                _bestandteilModell = new BrennstoffBestandteilCtrl().Read(_projektId, _gewaehlt.ID);
                _stand.Bestandteile = AusBrennstoffModell(_bestandteilModell);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Zerlegungsblock konnte nicht aufgebaut werden: " + ex.Message);
                _bestandteilModell = null;
            }
        }

        /// <summary>
        /// Preismodelle, deren Träger eine Preiszerlegung nach § 4.1 bekommen —
        /// wortgleich aus <c>ucFuelSettings.PREISMODELLE_BRENNSTOFF</c>.
        /// </summary>
        private static readonly string[] PREISMODELLE_BRENNSTOFF =
        {
            "GASEOUS_FUEL", "LIQUID_FUEL", "SOLID_FUEL", "ANIMAL_FAT"
        };

        private static StrompreisDetailsStand AusStromModell(StrompreisZerlegungModel m)
        {
            return new StrompreisDetailsStand
            {
                Beschaffung = m.Beschaffung, BeschaffungAktiv = m.Beschaffung_Aktiv,
                Vertrieb = m.Vertrieb, VertriebAktiv = m.Vertrieb_Aktiv,
                Netzentgelt = m.Netzentgelt, NetzentgeltAktiv = m.Netzentgelt_Aktiv,
                Stromsteuer = m.Stromsteuer, StromsteuerAktiv = m.Stromsteuer_Aktiv,
                Konzession = m.Konzession, KonzessionAktiv = m.Konzession_Aktiv,
                Umlagen = m.Umlagen, UmlagenAktiv = m.Umlagen_Aktiv,
                UmlagenEinzeln = m.Umlagen_Einzeln,
                UmlageKwkg = m.Umlage_KWKG, UmlageKwkgAktiv = m.Umlage_KWKG_Aktiv,
                UmlageOffshore = m.Umlage_Offshore, UmlageOffshoreAktiv = m.Umlage_Offshore_Aktiv,
                UmlageStromNev19 = m.Umlage_StromNEV19,
                UmlageStromNev19Aktiv = m.Umlage_StromNEV19_Aktiv
            };
        }

        private static void InStromModell(StrompreisDetailsStand s, StrompreisZerlegungModel m)
        {
            m.Beschaffung = s.Beschaffung; m.Beschaffung_Aktiv = s.BeschaffungAktiv;
            m.Vertrieb = s.Vertrieb; m.Vertrieb_Aktiv = s.VertriebAktiv;
            m.Netzentgelt = s.Netzentgelt; m.Netzentgelt_Aktiv = s.NetzentgeltAktiv;
            m.Stromsteuer = s.Stromsteuer; m.Stromsteuer_Aktiv = s.StromsteuerAktiv;
            m.Konzession = s.Konzession; m.Konzession_Aktiv = s.KonzessionAktiv;
            m.Umlagen = s.Umlagen; m.Umlagen_Aktiv = s.UmlagenAktiv;
            m.Umlagen_Einzeln = s.UmlagenEinzeln;
            m.Umlage_KWKG = s.UmlageKwkg; m.Umlage_KWKG_Aktiv = s.UmlageKwkgAktiv;
            m.Umlage_Offshore = s.UmlageOffshore; m.Umlage_Offshore_Aktiv = s.UmlageOffshoreAktiv;
            m.Umlage_StromNEV19 = s.UmlageStromNev19;
            m.Umlage_StromNEV19_Aktiv = s.UmlageStromNev19Aktiv;
        }

        private static BrennstoffBestandteileStand AusBrennstoffModell(BrennstoffBestandteilModel m)
        {
            return new BrennstoffBestandteileStand
            {
                Energiesteuer = m.Energiesteuer, EnergiesteuerAktiv = m.Energiesteuer_Aktiv,
                CO2 = m.CO2, CO2Aktiv = m.CO2_Aktiv,
                Netzentgelt = m.Netzentgelt, NetzentgeltAktiv = m.Netzentgelt_Aktiv,
                Vertrieb = m.Vertrieb, VertriebAktiv = m.Vertrieb_Aktiv
            };
        }

        private static void InBrennstoffModell(BrennstoffBestandteileStand s,
                                               BrennstoffBestandteilModel m)
        {
            m.Energiesteuer = s.Energiesteuer; m.Energiesteuer_Aktiv = s.EnergiesteuerAktiv;
            m.CO2 = s.CO2; m.CO2_Aktiv = s.CO2Aktiv;
            m.Netzentgelt = s.Netzentgelt; m.Netzentgelt_Aktiv = s.NetzentgeltAktiv;
            m.Vertrieb = s.Vertrieb; m.Vertrieb_Aktiv = s.VertriebAktiv;
        }

        // =====================================================================
        // Emissionen (Etappe E3)
        // =====================================================================

        private void EmissionenAufbauen()
        {
            if (_gewaehlt == null || _stand == null) return;
            try
            {
                _emissionen = new EmissionenCtrl(_projektId, _gewaehlt.ID);
                _emissionen.Laden();
                EmissionszeilenSetzen();
            }
            catch (Exception ex)
            {
                // Ein misslungener Emissionsteil darf die Preispflege nicht
                // blockieren — dieselbe Zusage wie beim Umrechnungsblock.
                Console.WriteLine("Der Emissionsteil konnte nicht aufgebaut werden: " + ex.Message);
                _emissionen = null;
                _stand.EmissionenVerfuegbar = false;
            }
        }

        private void EmissionszeilenSetzen()
        {
            if (_emissionen == null || _stand == null) return;

            _stand.EmissionenVerfuegbar = _emissionen.Verfuegbar;
            _stand.ModusCo2e = string.Equals(_emissionen.Modus, DbWerte.EMISSION_MODUS_CO2E,
                                             StringComparison.Ordinal);
            _stand.ModusOrt = _projektId > 0
                ? T("KDLG_EM_MODUS_ORT_PROJEKT", "[Projekt]")
                : T("KDLG_EM_MODUS_ORT_VORGABE", "[globale Vorgabe]");

            // ET-D-2: Die Bilanzierungsmethode ist eine PROJEKTvorgabe. Im
            // Katalogkontext gibt es kein Projekt, also auch nichts zu wählen —
            // die Klappliste steht dort gesperrt, und der Grund steht darunter.
            _stand.ModusNurLesend = Katalogkontext;
            _stand.ModusHinweis = Katalogkontext
                ? T("KDLG_EM_MODUS_KATALOG",
                    "Die Bilanzierungsmethode ist eine Projektvorgabe und hier nur lesbar.")
                : "";
            _stand.EmissionsFussnote = T("KDLG_EM_FUSSNOTE",
                "Angezeigt wird nur die im Projekt gewählte Größe — CO₂ oder CO₂-Äquivalent. "
                + "SO₂ und NOx werden weiterhin geführt, aber nicht in dieser Tabelle gezeigt.");

            if (!_emissionen.Verfuegbar)
            {
                _stand.Emissionszeilen = Array.Empty<EmissionsFeldZeile>();
                return;
            }

            var zeilen = new List<EmissionsFeldZeile>();
            foreach (EmissionsZeile z in _emissionen.Zeilen)
            {
                zeilen.Add(new EmissionsFeldZeile
                {
                    Kuerzel = z.Kuerzel,
                    Name = z.Art.Name,
                    Einheit = z.Art.Einheit,
                    Wert = z.Wert,
                    Herkunft = z.QuelleText,
                    NurLesend = z.NurLesend
                });
            }
            _stand.Emissionszeilen = zeilen;
            EmissionsSummeSetzen();
        }

        private void EmissionsSummeSetzen()
        {
            if (_emissionen == null || _stand == null || !_emissionen.Verfuegbar) return;

            _stand.EmissionsSumme = string.Format(CultureInfo.CurrentCulture,
                T("KDLG_EM_SUMME", "CO₂-Äquivalent gesamt (ausgewählte Arten): {0} g/kWh"),
                _emissionen.SummeCo2eGKwh().ToString("N2", CultureInfo.CurrentCulture));

            _stand.EmissionsHinweis = _emissionen.SummeIstBereitsAequivalent()
                ? T("KDLG_EM_SUMME_F3",
                    "CO₂-Wert ist bereits Äquivalent — Summe = Wert, weitere Arten werden "
                    + "nicht aufsummiert.")
                : "";
        }

        /// <summary>
        /// Konzept F9: Die Rechner lesen bis Etappe E5 die alten Spalten — eine
        /// Kernart wird deshalb in ihr Bestandsfeld gespiegelt.
        /// </summary>
        private void KernwerteSpiegeln()
        {
            if (_emissionen == null || _stand == null || !_emissionen.Verfuegbar) return;
            foreach (EmissionsZeile z in _emissionen.Zeilen)
            {
                double wert = z.Wert ?? 0.0;
                if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_CO2, StringComparison.OrdinalIgnoreCase))
                    _stand.AltCO2 = wert;
                else if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_SO2, StringComparison.OrdinalIgnoreCase))
                    _stand.AltSO2 = wert;
                else if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_NOX, StringComparison.OrdinalIgnoreCase))
                    _stand.AltNOx = wert;
            }
        }

        // =====================================================================
        // Nachziehen: Einheiten, Formel, Effektivzeile, Blöcke
        // =====================================================================

        /// <summary>
        /// Nach jeder Feldänderung — der Sammelweg der Methoden
        /// <c>UpdatePricePerKWh</c>, <c>AktualisiereEffektivUndVerstoss</c>,
        /// <c>BestandteileNachziehen</c> und <c>EffektivpreisAnzeigen</c>.
        /// </summary>
        private void Nachziehen()
        {
            if (_stand == null || _gewaehlt == null) return;

            // DIE EINHEITEN (B1): Heiz- und Brennwert tragen IMMER
            // kWh/<Abrechnungseinheit> - sie sind Stoffwerte und folgen der
            // Preisbasis nicht. Der Leistungspreis steht in €/(kW·a) bzw.
            // €/(kW·Monat), der Grundpreis in €/a. Allein der ARBEITSPREIS
            // folgt der Klappliste.
            _stand.EinheitArbeitspreis = EnergietraegerPreiskarte.ArbeitspreisEinheit(
                AktuelleEinheit(), _gewaehlt.HasHi);
            _stand.EinheitHeizwert = EnergietraegerPreiskarte.HeizwertEinheit(_abrechnungseinheit);
            _stand.EinheitBrennwert = _stand.EinheitHeizwert;
            _stand.EinheitLeistungspreis = EnergietraegerPreiskarte.LeistungspreisEinheit(
                _stand.LeistungsModusMonat);

            // Basiswerte aus den Anzeigewerten (der Vorläufer hielt sie in den
            // Value-Changed-Handlern nach) - nur der Arbeitspreis wird dabei
            // zurückgerechnet.
            // Die Reihenfolge zählt (UR-1): Der Faktor der kWh-Basis IST der
            // Heizwert, also muss er stehen, bevor der Basispreis daraus fällt.
            _baseHi = _stand.Heizwert;
            _baseHs = _stand.Brennwert;
            _baseWork = EnergietraegerPreiskarte.BasisArbeitspreis(_stand.Arbeitspreis, Faktor());
            _basePower = _stand.Leistungspreis;
            _baseGround = _stand.Grundpreis;

            FormelSetzen();
            EffektivSetzen();
            RegelnSetzen();
            EmissionsSummeSetzen();
            LueckenSetzen();
        }

        /// <summary>
        /// Die Formelgruppe — gerechnet wird über die BASISWERTE (Arbeitspreis
        /// je Abrechnungseinheit ÷ Heizwert je Abrechnungseinheit), die Rechnung
        /// selbst steht im Kern (<see cref="EnergietraegerPreiskarte.Formel"/>).
        /// </summary>
        private void FormelSetzen()
        {
            EnergietraegerPreiskarte.Formelzeile z = EnergietraegerPreiskarte.Formel(
                _gewaehlt.HasHi, _abrechnungseinheit, AktuelleEinheit(), _baseWork, _baseHi);
            if (z == null) return;

            _stand.PreisJeKwh = z.PreisJeKwh;
            _stand.FormelText = z.Text;

            // ET-D: Die kurze Herleitung des Blocks „Preis und Heizwert" —
            // der Preis je kWh und, wenn beide Stoffwerte dastehen, der
            // Umrechnungsfaktor Hs/Hi.
            CultureInfo k = CultureInfo.CurrentCulture;
            string zeile = string.Format(k, T("ETV_HERLEITUNG_PREIS", "→ {0}/kWh"), z.PreisJeKwh);

            double? hshi = EnergietraegerPreiskarte.FaktorHsHi(_baseHi, _baseHs);
            if (hshi.HasValue)
                zeile += string.Format(k, T("ETV_HERLEITUNG_HSHI",
                    " · Umrechnungsfaktor Hs/Hi = {0}"), hshi.Value.ToString("N4", k));

            _stand.HerleitungPreis = zeile;
        }

        /// <summary>
        /// Effektivzeile und Verstosshinweis — beide über der ABRECHNUNGSEINHEIT,
        /// denn dort stehen Heiz- und Brennwert (B1).
        /// </summary>
        private void EffektivSetzen()
        {
            _stand.EffektivText = EnergietraegerPreiskarte.Effektivzeile(
                _abrechnungseinheit, _baseHi, _baseHs);

            string grund;
            _stand.VerstossText = EnergieEinheitenPruefung.ErreichtKwh(
                _abrechnungseinheit, _baseHi, _baseHs, _regeln, out grund) ? "" : grund;
        }

        private void RegelnSetzen()
        {
            var zeilen = new List<UmrechnungsregelZeile>();
            for (int i = 0; i < _regeln.Count; i++)
            {
                UmrechnungsRegel r = _regeln[i];
                zeilen.Add(new UmrechnungsregelZeile
                {
                    Nummer = i,
                    Name = r.Name ?? "",
                    Von = r.Von ?? "",
                    Nach = r.Nach ?? "",
                    Faktor = r.Faktor,
                    Aktiv = r.Aktiv
                });
            }
            _stand.Regeln = zeilen;
        }

        /// <summary>Übernimmt die Anzeigezeilen in die Speicherkopie (K3, L5).</summary>
        private void RegelnUebernehmen()
        {
            foreach (UmrechnungsregelZeile z in _stand.Regeln)
            {
                if (z.Nummer < 0 || z.Nummer >= _regeln.Count) continue;
                UmrechnungsRegel r = _regeln[z.Nummer];
                bool geaendert = !string.Equals(r.Name ?? "", z.Name, StringComparison.Ordinal) ||
                                 !string.Equals(r.Von ?? "", z.Von, StringComparison.Ordinal) ||
                                 !string.Equals(r.Nach ?? "", z.Nach, StringComparison.Ordinal) ||
                                 Math.Abs(r.Faktor - z.Faktor) > 1e-12 || r.Aktiv != z.Aktiv;
                r.Name = z.Name;
                r.Von = z.Von;
                r.Nach = z.Nach;
                r.Faktor = z.Faktor;
                r.Aktiv = z.Aktiv;
                // Jede Handänderung macht die Zeile zu einer gepflegten — ab dann
                // fasst sie keine Migration mehr an (L5).
                if (geaendert) r.UserEdited = true;
            }
        }

        /// <summary>Wortgleich aus <c>BtnRegelNeu_Click</c>.</summary>
        private void RegelNeu()
        {
            if (_gewaehlt == null) return;
            RegelnUebernehmen();
            bool gas = string.Equals(_gewaehlt.PricingModel, "GASEOUS_FUEL",
                                     StringComparison.OrdinalIgnoreCase);
            _regeln.Add(new UmrechnungsRegel
            {
                Id = 0,
                IdBrennstoff = _gewaehlt.ID_Brennstoff,
                Name = gas ? DbWerte.UMRECHNUNG_NAME_Z_FAKTOR : DbWerte.UMRECHNUNG_NAME_STANDARD,
                Von = _abrechnungseinheit,
                Nach = "",
                Faktor = 1,
                Aktiv = true,
                UserEdited = true
            });
            RegelnSetzen();
            EffektivSetzen();
        }

        /// <summary>
        /// DER RIEGEL (§ 4.3): Die Regel, die den Träger nach kWh trägt, lässt
        /// sich nicht abschalten. Gefragt wird der Prüfer — es gibt keine
        /// zweite Fassung der Fachregel.
        /// </summary>
        private bool DarfAbschalten(UmrechnungsregelZeile zeile, bool neu)
        {
            if (neu || _stand == null) return true;
            RegelnUebernehmen();

            string grund;
            if (EnergieEinheitenPruefung.DarfAbschalten(_abrechnungseinheit, _baseHi,
                                                        _baseHs, _regeln, zeile.Nummer,
                                                        out grund))
                return true;

            _stand.VerstossText = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.KOSTEN_UMRECHNUNG_RIEGEL, grund);
            return false;
        }

        /// <summary>
        /// Die Preisbasis sagt, in welcher Einheit der ARBEITSPREIS eingegeben
        /// wird — und nur er wird umgerechnet (B1). Heizwert, Brennwert,
        /// Leistungspreis und Grundpreis bleiben, wo sie sind: Der Index zeigt in
        /// die bereinigte Liste (W4-B-1) und verschiebt sich dabei nicht.
        /// </summary>
        private void PreisbasisWechseln(int index)
        {
            if (_stand == null || index < 0 || index >= _preisbasen.Count) return;
            if (_preisbasen[index].Faktor == 0.0) return;

            _stand.PreisbasisId = index;
            AnzeigeAusBasis();
            Nachziehen();
        }

        /// <summary>
        /// Trägt die Summe der Anteile in das Arbeitspreisfeld ein — der Rückweg
        /// von ct/kWh in die Abrechnungseinheit. Ohne Heizwert gibt es keinen
        /// Rückweg; dann bleibt das Feld, wie es war.
        ///
        /// <para><b>Ein Weg für beide Blöcke.</b> Ein Träger ist entweder Strom
        /// („Strompreis Details", SP-E-2) oder Brennstoff („Preisbestandteile",
        /// B2) — nie beides. Der Knopf heißt in beiden Fällen gleich, tut
        /// dasselbe und schreibt nur das Kartenfeld; gespeichert wird mit
        /// „Speichern".</para>
        /// </summary>
        private void ArbeitspreisAusBestandteilen()
        {
            if (_stand == null) return;

            double ctKwh;
            if (_stand.Zerlegung != null && _zerlegungModell != null)
            {
                InStromModell(_stand.Zerlegung, _zerlegungModell);
                ctKwh = StrompreisZerlegungCtrl.AlsPreiszerlegung(_zerlegungModell).SummeAktivCtKwh;
            }
            else if (_stand.Bestandteile != null && _bestandteilModell != null)
            {
                InBrennstoffModell(_stand.Bestandteile, _bestandteilModell);
                ctKwh = BrennstoffBestandteilCtrl.AlsPreiszerlegung(_bestandteilModell).SummeAktivCtKwh;
            }
            else return;

            double jeEinheit;

            if (!_gewaehlt.HasHi) jeEinheit = ctKwh / 100.0;
            else
            {
                if (_baseHi <= 0.0) return;
                jeEinheit = ctKwh / 100.0 * _baseHi;
            }

            // jeEinheit steht je ABRECHNUNGSEINHEIT; angezeigt wird es in der
            // gewählten Preisbasis (B1).
            _baseWork = jeEinheit;
            _stand.Arbeitspreis = EnergietraegerPreiskarte.AnzeigeArbeitspreis(_baseWork, Faktor());
            Nachziehen();
        }

        /// <summary>Der Arbeitspreis in ct/kWh — wortgleich aus <c>ArbeitspreisInCtKwh</c>.</summary>
        private double ArbeitspreisCtKwh()
        {
            try
            {
                if (_gewaehlt == null || !_gewaehlt.HasHi) return _baseWork * 100.0;
                if (_baseHi <= 0.0) return 0.0;
                return _baseWork / _baseHi * 100.0;
            }
            catch { return 0.0; }
        }

        // =====================================================================
        // Speichern (Ä14)
        // =====================================================================

        private string _speichernGrund = "";

        private string SpeichernGrund() { return _speichernGrund; }

        /// <summary>
        /// Ä14: „OK" und „Speichern" schreiben die offene Trägerkarte. Im
        /// Projektkontext nur, wenn der Träger dem Projekt zugeordnet ist; im
        /// Katalogkontext über den Katalogzweig (Ä9).
        /// </summary>
        private bool Speichern()
        {
            _speichernGrund = "";
            if (_stand == null || _gewaehlt == null) return true;

            // ETAPPE K3: die BLOCKIERENDE Prüfung. Der Träger muss kWh erreichen.
            string grund;
            if (!EnergieEinheitenPruefung.ErreichtKwh(_abrechnungseinheit, _baseHi,
                                                      _baseHs, _regeln, out grund))
            {
                _stand.VerstossText = grund;
                _speichernGrund = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.KOSTEN_UMRECHNUNG_SPEICHERN_ABGELEHNT, grund);
                return false;
            }

            if (_projektId > 0 && !EnergietraegerPreisCtrl.ImProjekt(_projektId, _gewaehlt.ID))
            {
                // ET-3 (08.09.2026): Ein verwendeter, noch nicht zugeordneter Traeger wird beim
                // Speichern ZUGEORDNET - bis hierher schrieb dieser Weg nichts und meldete
                // trotzdem "gespeichert".
                if (!EnergietraegerKatalogCtrl.InsProjekt(_projektId, _gewaehlt.ID))
                {
                    _speichernGrund = T("KDLG_ET_ZUORDNUNG_FEHLGESCHLAGEN",
                                        "Der Träger ließ sich dem Projekt nicht zuordnen.");
                    return false;
                }
                ListeLaden();
            }

            try
            {
                RegelnUebernehmen();
                EnergietraegerPreisCtrl.RegelnSpeichern(_regeln);
                WerteSpeichern();
                EmissionenSpeichern();
            }
            catch (Exception ex)
            {
                _speichernGrund = ex.Message;
                return false;
            }

            // B2: Die Tabelle zeigt nach dem Speichern den neuen Stand - der
            // Vorlaeufer las die Historie ebenfalls nach jedem Schreiben neu.
            _stand.UebernahmeHinweis = "";
            HistorieLaden();

            // Was jetzt in der Datenbank steht, ist die neue Wahrheit: Die
            // Lückenzeilen werden daran neu gemessen, damit der Hinweis nach dem
            // Speichern stimmt - und nach dem Speichern DERSELBE ist wie davor,
            // wenn nichts eingetragen wurde (das Speichern sperrt er nicht).
            KetteLesen();
            Nachziehen();
            return true;
        }

        /// <summary>
        /// Wortgleich aus <c>SpeichereWerte</c>, nur über den Controller.
        ///
        /// <para><b>Geschrieben werden die BASISWERTE</b> — Arbeitspreis, Heiz-
        /// und Brennwert je Abrechnungseinheit (B1). Die Preisbasis ist eine
        /// Eingabehilfe, keine Speichereinheit; sie geht nur als
        /// <c>ID_Umrechnung</c> mit.</para>
        /// </summary>
        private void WerteSpeichern()
        {
            KernwerteSpiegeln();

            var preis = new EnergietraegerPreisCtrl.Preisstand
            {
                Arbeitspreis = _baseWork,
                Grundpreis = _baseGround,
                Leistungspreis = _basePower,
                Hi = _baseHi,
                Hs = _baseHs,
                CO2 = _stand.AltCO2,
                SO2 = _stand.AltSO2,
                NOx = _stand.AltNOx,
                IdUmrechnung = EnergietraegerPreisCtrl.UmrechnungsId(AktuelleUmrechnung()),
                Basiseinheit = _stand.Basiseinheit,
                // ETAPPE E7c (Schritt F): der Kartenzustand als Einheitentext — auch
                // dann, wenn der Brennstoff keine Regel nach kWh fuehrt (U32).
                Preisbasis = _projektId > 0 ? AktuelleEinheit() : null
            };

            if (_projektId <= 0)
            {
                EnergietraegerPreisCtrl.Katalogwerte(_gewaehlt.ID, preis);

                _gewaehlt.price_work = preis.Arbeitspreis;
                _gewaehlt.price_base = preis.Grundpreis;
                _gewaehlt.price_power = preis.Leistungspreis;
                _gewaehlt.HiKwhPerUnit = preis.Hi;
                _gewaehlt.HsKwhPerUnit = preis.Hs;
                _gewaehlt.CO2 = preis.CO2;
                _gewaehlt.SO2 = preis.SO2;
                _gewaehlt.NOx = preis.NOx;

                // B2, BENANNTE ABLEHNUNG: Im Katalogkontext entsteht KEINE
                // Historienzeile. energy_price.ID_Projekt trägt einen
                // Fremdschlüssel auf Tab_Projekt.ID, und das Projekt 0 gibt es
                // nicht — ein Schreibversuch endete in einem
                // Fremdschlüsselfehler statt in einer Zeile. Die Karte sagt das
                // unter der Tabelle, statt es still zu übergehen; die Werte
                // selbst stehen in der Katalogzeile.
                AnkerSetzen(preis);
                return;
            }

            HistorieSchreibenWennGeaendert(preis, _projektId);
            EnergietraegerPreisCtrl.Projektwerte(_projektId, _gewaehlt.ID, preis);

            // Q11: die Leistungspreis-Staffel des Stromtraegers - in DIESELBE Zeile,
            // deshalb erst nach dem Upsert (wie die beiden Preisbloecke darunter).
            // Ein Schreibfehler bricht das Speichern ab, statt still zu verschwinden.
            if (_stand.MitStaffel &&
                !EnergietraegerPreisCtrl.StaffelSchreiben(_projektId, _gewaehlt.ID, new LeistungspreisStaffel
                {
                    GrenzeKW = _stand.StaffelGrenze,
                    Preis1EurKWa = _stand.StaffelPreis1,
                    Preis2EurKWa = _stand.StaffelPreis2
                }))
                throw new InvalidOperationException(
                    T("ETV_STAFFEL_SPEICHERFEHLER",
                      "Die Leistungspreis-Staffel ließ sich nicht speichern."));

            // AP4/B2: Die beiden Blöcke schreiben in DIESELBE Zeile und deshalb
            // ERST JETZT — vor dem Upsert gäbe es beim ersten Speichern keine.
            if (_stand.Zerlegung != null && _zerlegungModell != null)
            {
                InStromModell(_stand.Zerlegung, _zerlegungModell);
                new StrompreisZerlegungCtrl().Update(_zerlegungModell);
            }
            if (_stand.Bestandteile != null && _bestandteilModell != null)
            {
                InBrennstoffModell(_stand.Bestandteile, _bestandteilModell);
                new BrennstoffBestandteilCtrl().Update(_bestandteilModell);
            }
        }

        private void AnkerSetzen(EnergietraegerPreisCtrl.Preisstand p)
        {
            _dbWork = p.Arbeitspreis; _dbGround = p.Grundpreis; _dbPower = p.Leistungspreis;
            _dbHi = p.Hi; _dbHs = p.Hs;
            _dbCO2 = p.CO2; _dbSO2 = p.SO2; _dbNOx = p.NOx;
        }

        /// <summary>
        /// B2: Eine Historienzeile entsteht nur, wenn sich gegenüber den
        /// DB-Ankern etwas geändert hat — zum Datum aus dem Feld „Gültig ab".
        /// Ein zweites Speichern am selben Tag AKTUALISIERT die Zeile
        /// (<c>HistorieSchreiben</c>), es legt keine zweite an.
        /// </summary>
        private void HistorieSchreibenWennGeaendert(EnergietraegerPreisCtrl.Preisstand preis,
                                                    int projektId)
        {
            bool geaendert = Math.Abs(preis.Arbeitspreis - _dbWork) > 0.0001 ||
                             Math.Abs(preis.Hi - _dbHi) > 0.0001 ||
                             Math.Abs(preis.Hs - _dbHs) > 0.0001 ||
                             Math.Abs(preis.Grundpreis - _dbGround) > 0.01 ||
                             Math.Abs(preis.Leistungspreis - _dbPower) > 0.01 ||
                             Math.Abs(preis.CO2 - _dbCO2) > 0.01 ||
                             Math.Abs(preis.SO2 - _dbSO2) > 0.01 ||
                             Math.Abs(preis.NOx - _dbNOx) > 0.01;
            if (!geaendert) return;

            DateTime datum = _stand.GueltigAb.HasValue
                ? _stand.GueltigAb.Value.ToDateTime(TimeOnly.MinValue) : DateTime.Now;
            EnergietraegerPreisCtrl.HistorieSchreiben(_gewaehlt.ID, projektId, datum, preis);
            AnkerSetzen(preis);
        }

        /// <summary>
        /// B2 (Anwenderbefund 14.09.2026, „die Eingaben werden nicht gespeichert"):
        /// Die Tabelle blieb leer, weil <c>Stand.Historie</c> NIE befüllt wurde —
        /// <see cref="EnergietraegerPreisCtrl.Historie"/> hatte in der ganzen
        /// Trägerkarte keinen Aufrufer. Sie wird jetzt beim Trägerwechsel und
        /// nach jedem erfolgreichen Speichern gelesen, im Projektkontext die
        /// Zeilen des Projekts, im Katalogkontext die unter <c>ID_Projekt = 0</c>.
        /// </summary>
        private void HistorieLaden()
        {
            if (_stand == null || _gewaehlt == null) return;
            try
            {
                var zeilen = new List<PreishistorieZeile>();
                foreach (EnergietraegerPreisCtrl.Historienzeile h in
                         EnergietraegerPreisCtrl.Historie(_gewaehlt.ID, _projektId))
                {
                    zeilen.Add(new PreishistorieZeile(
                        h.GueltigAb == DateTime.MinValue
                            ? "" : h.GueltigAb.ToString("d", CultureInfo.CurrentCulture),
                        Zahl(h.Heizwert, "N2"),
                        h.Basiseinheit ?? "",
                        Zahl(h.Arbeitspreis, "N4"),
                        Zahl(h.Grundpreis, "N2"),
                        Zahl(h.Leistungspreis, "N2"),
                        h.Id));
                }
                _stand.Historie = zeilen;
                _stand.HistorieHinweis = Katalogkontext
                    ? T("ETV_HISTORIE_NUR_PROJEKT",
                        "Die Preishistorie wird je Projekt geführt — im Katalog gilt die "
                        + "Katalogzeile selbst.")
                    : "";
            }
            catch (Exception ex)
            {
                // Eine fehlende Historientabelle darf die Preispflege nicht
                // blockieren — dieselbe Zusage wie beim Umrechnungsblock.
                Console.WriteLine("Die Preishistorie konnte nicht gelesen werden: " + ex.Message);
                _stand.Historie = Array.Empty<PreishistorieZeile>();
            }
        }

        private string _historieLoeschGrund = "";

        /// <summary>Der Grund, wenn <see cref="HistorieLoeschen"/> abgelehnt hat.</summary>
        private string HistorieLoeschenGrund() { return _historieLoeschGrund; }

        /// <summary>
        /// Löscht EINE Zeile der Preishistorie (Anwenderwunsch 14.09.2026,
        /// „historische Energieträger werte sollen gelöscht werden können").
        ///
        /// <para><b>Nur im Projektkontext.</b> Im Katalog führt die Karte gar
        /// keine Historie — dort gilt die Katalogzeile selbst; die Ablehnung
        /// nennt denselben Satz, der unter der leeren Tabelle steht, statt
        /// still nichts zu tun.</para>
        ///
        /// <para>Ein gesperrter Datenbestand (Lesemodus) meldet sich aus der
        /// Schreibnaht als Ausnahme; ihr Text ist dann der Grund — dieselbe
        /// Zusage wie beim Speichern.</para>
        /// </summary>
        /// <returns><c>true</c> = gelöscht, die Tabelle steht neu; sonst
        /// <c>false</c> mit <see cref="HistorieLoeschenGrund"/>.</returns>
        private bool HistorieLoeschen(PreishistorieZeile zeile)
        {
            _historieLoeschGrund = "";
            if (zeile == null || _stand == null || _gewaehlt == null) return false;

            if (Katalogkontext)
            {
                _historieLoeschGrund = T("ETV_HISTORIE_NUR_PROJEKT",
                    "Die Preishistorie wird je Projekt geführt — im Katalog gilt die "
                    + "Katalogzeile selbst.");
                return false;
            }

            try
            {
                if (EnergietraegerPreisCtrl.HistorieLoeschen(zeile.Id, _gewaehlt.ID, _projektId) <= 0)
                {
                    _historieLoeschGrund = T("ETV_HISTORIE_LOESCH_FEHLT",
                        "Dieser Preisstand ist nicht mehr vorhanden.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _historieLoeschGrund = ex.Message;
                return false;
            }

            HistorieLaden();
            return true;
        }

        /// <summary>Eine Historienzahl; <c>null</c> bleibt leer statt „0,00" zu lesen.</summary>
        private static string Zahl(double? wert, string format)
        {
            return wert.HasValue ? wert.Value.ToString(format, CultureInfo.CurrentCulture) : "";
        }

        /// <summary>
        /// B3 (Anwenderwunsch 14.09.2026): Die Katalogwerte der Administration in
        /// das Projekt holen — Arbeits-, Grund- und Leistungspreis, Heiz- und
        /// Brennwert und die drei Emissionswerte des Trägers.
        ///
        /// <para><b>Eine einmalige KOPIE</b> (Anwenderentscheid 14.09.2026), kein
        /// „dem Katalog folgen": Die Werte stehen danach in den Feldern und sind
        /// dort änderbar; geschrieben wird erst mit „Speichern" bzw. „OK",
        /// und dabei entsteht die Historienzeile.</para>
        ///
        /// <para>Die Preisbasis geht auf die ABRECHNUNGSEINHEIT zurück — in ihr
        /// steht die Katalogzeile.</para>
        /// </summary>
        private void KatalogwerteUebernehmen()
        {
            if (_stand == null || _gewaehlt == null || _projektId <= 0) return;

            _baseWork = _gewaehlt.price_work;
            _baseGround = _gewaehlt.price_base;
            _basePower = _gewaehlt.price_power;
            _baseHi = _gewaehlt.HiKwhPerUnit;
            _baseHs = _gewaehlt.HsKwhPerUnit;

            _stand.PreisbasisId = IndexZuEinheit(_abrechnungseinheit);
            AnzeigeAusBasis();

            EmissionswerteUebernehmen();

            _stand.UebernahmeHinweis = T("ETV_KATALOGWERTE_UEBERNOMMEN",
                "Katalogwerte übernommen — noch nicht gespeichert.");
            Nachziehen();
        }

        /// <summary>
        /// Die drei Kernarten aus der Katalogzeile. Steht der Artenkatalog
        /// (Migrationsschritt 57), geht der Wert durch <c>EmissionenCtrl</c> —
        /// nur so liest ihn <see cref="KernwerteSpiegeln"/> beim Speichern
        /// zurück; sonst zählen die drei Bestandsfelder.
        /// </summary>
        private void EmissionswerteUebernehmen()
        {
            _stand.AltCO2 = _gewaehlt.CO2;
            _stand.AltSO2 = _gewaehlt.SO2;
            _stand.AltNOx = _gewaehlt.NOx;

            if (_emissionen == null || !_emissionen.Verfuegbar) return;

            Uebernehmen(DbWerte.EMISSIONSART_CO2, _gewaehlt.CO2);
            Uebernehmen(DbWerte.EMISSIONSART_SO2, _gewaehlt.SO2);
            Uebernehmen(DbWerte.EMISSIONSART_NOX, _gewaehlt.NOx);
            EmissionszeilenSetzen();
        }

        private void Uebernehmen(string kuerzel, double wert)
        {
            EmissionsZeile ziel = ZeileZuKuerzel(kuerzel);
            if (ziel == null || ziel.NurLesend) return;
            _emissionen.WertEingeben(ziel, wert.ToString("0.####", CultureInfo.CurrentCulture));
        }

        private void EmissionenSpeichern()
        {
            if (_emissionen == null || !_emissionen.Verfuegbar || _stand == null) return;
            try
            {
                foreach (EmissionsFeldZeile z in _stand.Emissionszeilen)
                {
                    if (z.NurLesend) continue;
                    EmissionsZeile ziel = ZeileZuKuerzel(z.Kuerzel);
                    if (ziel != null)
                        _emissionen.WertEingeben(ziel, z.Wert.HasValue
                            ? z.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture) : "");
                }
                _emissionen.Modus = _stand.ModusCo2e
                    ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
                _emissionen.Speichern();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Emissionswerte konnten nicht gespeichert werden: " + ex.Message);
            }
        }

        private EmissionsZeile ZeileZuKuerzel(string kuerzel)
        {
            if (_emissionen == null) return null;
            foreach (EmissionsZeile z in _emissionen.Zeilen)
                if (string.Equals(z.Kuerzel, kuerzel, StringComparison.OrdinalIgnoreCase)) return z;
            return null;
        }

        // =====================================================================
        // Fehlende Werte und die Übernahme aus der Kategorie
        //
        // DIE LÜCKE WIRD SICHTBAR, WO SIE ENTSTEHT. Ein Arbeitspreis 0, ein
        // Leistungspreis 0 oder ein fehlender CO₂-Wert ließen sich bis hierher
        // ohne jede Meldung speichern; auffallen tat es erst beim Rechnen — beim
        // CO₂ als stille 0 in der Emissionsbilanz, beim Preis als
        // „Energiekosten nicht bestimmbar". Die Karte sagt es jetzt am Ort der
        // Pflege.
        //
        // DER HINWEIS SPERRT NICHTS. Ein halbgepflegter Träger muss sich anlegen
        // lassen; Speichern bleibt möglich und meldet denselben Hinweis.
        //
        // KEIN AUTOMATISCHER RÜCKFALL. Der Übernahmeweg ist ein Bedienweg: Er
        // legt Kandidaten vor, und erst die Bestätigung des Anwenders schreibt.
        // Im Rechenweg bleibt ein Träger ohne Wert eine Datenlücke.
        // =====================================================================

        /// <summary>
        /// Was die Lesekette zu den drei Größen sagt — EINMAL je Trägerwechsel.
        /// Befragt wird <see cref="EnergietraegerRueckfall"/>, damit es bei der
        /// einen Kette bleibt (CO₂ über <c>Emissionsquelle.Co2Gepflegt</c>, die
        /// Preise über die Vorrangkette der Wirtschaftlichkeit).
        /// </summary>
        private void KetteLesen()
        {
            _co2AusKette = false;
            _arbeitspreisAusKette = false;
            _leistungspreisAusKette = false;
            if (_gewaehlt == null) return;

            try
            {
                _co2AusKette = EnergietraegerRueckfall
                    .Wert(_projektId, _gewaehlt.ID, Rueckfallgroesse.Co2).HasValue;
                _arbeitspreisAusKette = EnergietraegerRueckfall
                    .Wert(_projektId, _gewaehlt.ID, Rueckfallgroesse.Arbeitspreis).HasValue;
                _leistungspreisAusKette = EnergietraegerRueckfall
                    .Wert(_projektId, _gewaehlt.ID, Rueckfallgroesse.Leistungspreis).HasValue;
            }
            catch { }
        }

        /// <summary>
        /// Die Zeilen der Karte zu den drei Größen: fehlende Werte mit ihrem
        /// Übernahmeweg, geliehene mit ihrer Herleitung.
        /// </summary>
        private void LueckenSetzen()
        {
            if (_stand == null || _gewaehlt == null) return;

            var liste = new List<Wertluecke>();

            Wertzeile(liste, EnergietraegerRueckfall.GROESSE_ARBEITSPREIS,
                _baseWork > 0.0 || _arbeitspreisAusKette,
                T("ETV_LUECKE_ARBEITSPREIS",
                  "Für diesen Energieträger ist kein Arbeitspreis gepflegt. Ohne ihn lässt "
                  + "sich die Wirtschaftlichkeit nicht rechnen."));

            // Nur Träger, die überhaupt einen Leistungspreis führen. Eine
            // gepflegte Saisonreihe (FK6a) gilt vor dem konstanten Satz - dann
            // ist der Leistungspreis gepflegt, auch wenn das Feld 0 zeigt.
            if (_gewaehlt.HasPowerPrice)
            {
                Wertzeile(liste, EnergietraegerRueckfall.GROESSE_LEISTUNGSPREIS,
                    _basePower > 0.0 || _leistungspreisAusKette
                        || (_stand.ReihenStatus ?? "").Length > 0,
                    T("ETV_LUECKE_LEISTUNGSPREIS",
                      "Für diesen Energieträger ist kein Leistungspreis gepflegt."));
            }

            double? co2 = KartenCo2();
            Wertzeile(liste, EnergietraegerRueckfall.GROESSE_CO2,
                (co2.HasValue && co2.Value > 0.0) || _co2AusKette,
                T("ETV_LUECKE_CO2",
                  "Für diesen Energieträger ist kein CO₂-Wert gepflegt. Die Emissionsbilanz "
                  + "rechnet ihn mit 0."));

            _stand.Wertluecken = liste;
        }

        /// <summary>
        /// Eine Zeile entsteht nur, wenn es etwas zu sagen gibt: eine offene Lücke
        /// oder ein geliehener Wert. Der Übernahmeweg steht allein im
        /// PROJEKTkontext — geschrieben wird in die Projektübersteuerung, und die
        /// gibt es im Katalog nicht.
        /// </summary>
        private void Wertzeile(List<Wertluecke> liste, string groesse, bool gepflegt,
                               string hinweis)
        {
            string leih;
            _leihzeilen.TryGetValue(groesse, out leih);
            bool geliehen = !string.IsNullOrEmpty(leih);
            if (gepflegt && !geliehen) return;

            liste.Add(new Wertluecke
            {
                Groesse = groesse,
                Hinweis = gepflegt ? "" : hinweis,
                KnopfText = gepflegt || _projektId <= 0
                    ? ""
                    : T("ETV_LUECKE_BTN", "Wert aus der Kategorie übernehmen…"),
                Leihzeile = leih ?? ""
            });
        }

        /// <summary>
        /// Der CO₂-Wert, wie er JETZT IN DER KARTE steht: die Zeile des
        /// Artenkatalogs, sonst das Bestandsfeld. <c>null</c> = keine Zeile.
        /// </summary>
        private double? KartenCo2()
        {
            if (_stand == null) return null;
            if (_stand.EmissionenVerfuegbar && _stand.Emissionszeilen != null)
            {
                foreach (EmissionsFeldZeile z in _stand.Emissionszeilen)
                {
                    if (string.Equals(z.Kuerzel, DbWerte.EMISSIONSART_CO2,
                                      StringComparison.OrdinalIgnoreCase))
                        return z.Wert;
                }
            }
            return _stand.AltCO2;
        }

        /// <summary>Die offenen Lücken in einem Satz; leer = keine.</summary>
        private string SpeichernHinweis()
        {
            if (_stand == null) return "";
            var teile = new List<string>();
            foreach (Wertluecke l in _stand.Wertluecken)
                if (!string.IsNullOrEmpty(l.Hinweis)) teile.Add(l.Hinweis);
            return teile.Count == 0 ? "" : string.Join(" ", teile);
        }

        /// <summary>
        /// Die RÜCKFRAGE des Übernahmewegs: die Träger derselben Kategorie, die
        /// den gesuchten Wert tragen, jeder mit seinem Wert und dessen Einheit.
        /// Eine leere Kandidatenliste ist eine gültige Antwort — dann sagt der Weg
        /// das und bietet nichts an.
        /// </summary>
        private Uebernahmewahl Kandidatenwahl(string schluessel)
        {
            var wahl = new Uebernahmewahl { Groesse = schluessel ?? "" };

            Rueckfallgroesse groesse;
            if (_gewaehlt == null || _projektId <= 0
                || !EnergietraegerRueckfall.Groesse(schluessel, out groesse)) return wahl;

            string kategorie = EnergietraegerRueckfall.KategorieName(
                EnergietraegerRueckfall.Kategorie(_gewaehlt.ID));
            string name = Groessenname(groesse);

            wahl.Titel = string.Format(CultureInfo.CurrentCulture,
                T("ETV_LUECKE_TITEL", "{0} aus der Kategorie übernehmen"), name);
            wahl.Frage = string.Format(CultureInfo.CurrentCulture,
                T("ETV_LUECKE_FRAGE",
                  "Welcher Energieträger der Kategorie „{0}“ soll den Wert stellen? Der "
                  + "gewählte Wert wird in dieses Projekt übernommen; der Katalog bleibt "
                  + "unverändert."),
                kategorie);
            wahl.LeerText = string.Format(CultureInfo.CurrentCulture,
                T("ETV_LUECKE_LEER",
                  "Kein Energieträger der Kategorie „{0}“ trägt einen {1} — es gibt nichts "
                  + "zu übernehmen."),
                kategorie, name);

            var eintraege = new List<ValueTuple<int, string>>();
            foreach (Rueckfallkandidat k in
                     EnergietraegerRueckfall.Kandidaten(_projektId, _gewaehlt.ID, groesse))
            {
                eintraege.Add(new ValueTuple<int, string>(k.TraegerId,
                    string.Format(CultureInfo.CurrentCulture,
                        T("ETV_LUECKE_KANDIDAT", "{0} — {1} {2}"),
                        k.Name, Wertzahl(k.Wert), k.Einheit)));
            }
            wahl.Kandidaten = eintraege;
            return wahl;
        }

        /// <summary>
        /// Die BESTÄTIGTE Übernahme: Der Wert des gewählten Trägers geht in die
        /// Projektübersteuerung dieses Trägers und in die Felder der Karte.
        /// <c>false</c> = nichts geschrieben (der gewählte Träger ist kein
        /// Kandidat mehr, oder das Schreiben ist gescheitert).
        ///
        /// <para>Der gewählte Träger wird gegen die FRISCH gelesene Kandidatenliste
        /// gehalten — was nicht angeboten wurde, wird auch nicht übernommen. Damit
        /// gilt die Einheitenprüfung des Kerns auch für diesen Weg.</para>
        /// </summary>
        private bool LueckeUebernehmen(string schluessel, int geberId)
        {
            Rueckfallgroesse groesse;
            if (_stand == null || _gewaehlt == null || _projektId <= 0 || geberId <= 0
                || !EnergietraegerRueckfall.Groesse(schluessel, out groesse)) return false;

            Rueckfallkandidat geber = null;
            foreach (Rueckfallkandidat k in
                     EnergietraegerRueckfall.Kandidaten(_projektId, _gewaehlt.ID, groesse))
            {
                if (k.TraegerId == geberId) { geber = k; break; }
            }
            if (geber == null) return false;

            if (!EnergietraegerRueckfall.Uebernehmen(_projektId, _gewaehlt.ID,
                                                     groesse, geber.Wert)) return false;

            switch (groesse)
            {
                case Rueckfallgroesse.Arbeitspreis:
                    _baseWork = geber.Wert;
                    AnzeigeAusBasis();
                    break;
                case Rueckfallgroesse.Leistungspreis:
                    _basePower = geber.Wert;
                    AnzeigeAusBasis();
                    break;
                default:
                    _stand.AltCO2 = geber.Wert;
                    Uebernehmen(DbWerte.EMISSIONSART_CO2, geber.Wert);
                    EmissionszeilenSetzen();
                    break;
            }

            // Ein übernommener Wert ist ab jetzt ein gepflegter Wert — aber man
            // muss sehen können, wie er dorthin kam.
            _leihzeilen[EnergietraegerRueckfall.Schluessel(groesse)] =
                string.Format(CultureInfo.CurrentCulture,
                    T("ETV_LUECKE_LEIHZEILE",
                      "{0} {1} {2} aus der Kategorie „{3}“ übernommen — Energieträger „{4}“."),
                    Groessenname(groesse), Wertzahl(geber.Wert), geber.Einheit,
                    EnergietraegerRueckfall.KategorieName(
                        EnergietraegerRueckfall.Kategorie(_gewaehlt.ID)),
                    geber.Name);

            KetteLesen();
            Nachziehen();
            return true;
        }

        /// <summary>Der Name einer Größe in Worten.</summary>
        private static string Groessenname(Rueckfallgroesse groesse)
        {
            switch (groesse)
            {
                case Rueckfallgroesse.Arbeitspreis:
                    return T("ETV_GROESSE_ARBEITSPREIS", "Arbeitspreis");
                case Rueckfallgroesse.Leistungspreis:
                    return T("ETV_GROESSE_LEISTUNGSPREIS", "Leistungspreis");
                default:
                    return T("ETV_GROESSE_CO2", "CO₂-Wert");
            }
        }

        /// <summary>Ein Preis oder Faktor, wie er in der Rückfrage steht — vier
        /// Nachkommastellen, damit ein ct-Betrag nicht gerundet daherkommt.</summary>
        private static string Wertzahl(double wert)
        {
            return wert.ToString("0.####", CultureInfo.CurrentCulture);
        }

        // =====================================================================
        // Katalogpflege (Ä9/Ä10)
        // =====================================================================

        private bool StammSchreiben(string name, int? gruppe)
        {
            if (_gewaehlt == null) return false;
            if (!EnergietraegerKatalogCtrl.Umbenennen(_gewaehlt.ID, name, GruppenName(gruppe)))
                return false;
            ListeLaden();
            return true;
        }

        private IReadOnlyDictionary<string, object> NamensGaben()
        {
            return NamensabfrageGaben.Gaben(
                T("KDLG_ET_NEU_TITEL", "Neuer Energieträger"),
                T("KDLG_ET_NEU_NAME", "Bezeichnung des neuen Trägers:"),
                T("KDLG_ET_NEU_VORGABE", "Neuer Energieträger"),
                T("NAMD_MSG_LEER", "Bitte einen Namen eingeben."));
        }

        private int TraegerNeu(string name)
        {
            int id = EnergietraegerKatalogCtrl.Neu(name, null);
            if (id > 0) { ListeLaden(); TraegerWaehlen(id); }
            return id;
        }

        private int TraegerVariante()
        {
            if (_gewaehlt == null) return 0;
            int id = EnergietraegerKatalogCtrl.Variante(_gewaehlt.ID);
            if (id > 0) { ListeLaden(); TraegerWaehlen(id); }
            return id;
        }

        private ValueTuple<bool, string> TraegerLoeschen()
        {
            if (_gewaehlt == null) return new ValueTuple<bool, string>(false, "");
            string grund;
            bool ok = EnergietraegerKatalogCtrl.Loeschen(_gewaehlt.ID, out grund);
            if (ok) { ListeLaden(); _gewaehlt = null; _stand = null; }
            return new ValueTuple<bool, string>(ok, grund ?? "");
        }

        private int InsProjekt(IReadOnlyList<int> ids)
        {
            int letzter = 0;
            foreach (int id in ids)
                if (EnergietraegerKatalogCtrl.InsProjekt(_projektId, id)) letzter = id;
            if (letzter > 0) { ListeLaden(); TraegerWaehlen(letzter); }
            return letzter;
        }

        /// <summary>
        /// ET‑6 (Anwenderbefund 16.09.2026): Entfernt der Anwender den Stromträger eines
        /// Projekts mit Wärmepumpe, Photovoltaik, Stromspeicher oder Heizstab, ordnet
        /// <c>StromTraegerSicherstellen</c> im selben Atemzug wieder einen zu — bis hierher
        /// STILL, und der Anwender hielt das für den Fehler, der gerade behoben wird.
        ///
        /// <para>Der Kern bleibt unberührt: <b>Die Wiederzuordnung selbst ist
        /// Anwenderentscheid ET‑2 vom 08.09.2026.</b> Sie wird nur benannt. Gemessen wird
        /// an den ZUGEORDNETEN Trägern vor und nach dem Entfernen — die Rückgabe von
        /// <c>StromTraegerSicherstellen</c> taugt dafür nicht, weil sie auch dann eine Id
        /// nennt, wenn der Träger längst zugeordnet war (idempotenter Fall).</para>
        /// </summary>
        private ValueTuple<bool, string> AusProjekt()
        {
            _stromZugeordnet = "";
            if (_gewaehlt == null) return new ValueTuple<bool, string>(false, "");

            int entfernt = _gewaehlt.ID;
            var vorher = new List<int>();
            foreach (EnergyCarrier c in _traeger)
                if (c.ID != entfernt && ListenZugeordnet(c.ID)) vorher.Add(c.ID);

            string grund;
            bool ok = EnergietraegerKatalogCtrl.AusProjektEntfernen(_projektId, entfernt, out grund);
            if (!ok) return new ValueTuple<bool, string>(false, grund ?? "");

            ListeLaden();
            foreach (EnergyCarrier c in _traeger)
            {
                if (!ListenZugeordnet(c.ID) || vorher.Contains(c.ID)) continue;
                _stromZugeordnet = c.Name;
                break;
            }
            _gewaehlt = null;
            _stand = null;
            return new ValueTuple<bool, string>(true, "");
        }

        /// <summary>ET‑6: der Träger, den das Entfernen nach sich gezogen hat; leer = keiner.</summary>
        private string StromZugeordnet() { return _stromZugeordnet; }

        // =====================================================================
        // Unterdialoge
        // =====================================================================

        private IReadOnlyDictionary<string, object> KostenprofilGaben()
        {
            var ctrl = new KostenprofilCtrl();
            var vorhandene = ctrl.ReadAllByProjekt(_projektId);
            int id = vorhandene.Count > 0 ? vorhandene[0].ID : 0;
            return KostenprofilHuelle.Gaben(_projektId, id);
        }

        private IReadOnlyDictionary<string, object> SaisonGaben()
        {
            return _gewaehlt == null
                ? new Dictionary<string, object>()
                : LeistungspreisReiheHuelle.Gaben(_projektId, _gewaehlt.ID, _gewaehlt.Name);
        }

        private EmissionskatalogHuelle.Aufruf _katalogAufruf;

        private IReadOnlyDictionary<string, object> EmissionskatalogGaben(string kuerzel)
        {
            if (_gewaehlt == null) return new Dictionary<string, object>();
            _katalogAufruf = EmissionskatalogHuelle.Gaben(_gewaehlt.ID, _gewaehlt.Name,
                                                          kuerzel ?? "",
                                                          !string.IsNullOrEmpty(kuerzel));
            return _katalogAufruf.Parameter;
        }

        /// <summary>
        /// Wortgleich aus <c>KatalogFuerZeile</c>/<c>KatalogVerwalten</c>: Der
        /// übernommene Wert lebt bis zum Speichern nur im Objekt (Ä12/Ä14); die
        /// Änderungsmerker gelten auch bei „Abbrechen" (A-16 aus Welle 3).
        /// </summary>
        private void EmissionskatalogAuswerten(EmissionskatalogErgebnis ergebnis)
        {
            if (_katalogAufruf == null || _emissionen == null) return;
            EmissionskatalogHuelle.Ergebnis erg = _katalogAufruf.Auswerten(ergebnis);
            _katalogAufruf = null;

            if (erg.Uebernommen != null)
            {
                EmissionsZeile ziel = ZeileZuKuerzel(ErsteZeileMitWert(erg.Uebernommen));
                if (ziel != null) _emissionen.KatalogwertUebernehmen(ziel, erg.Uebernommen);
            }
            if (erg.ArtenGeaendert || erg.WerteGeaendert)
                _emissionen.NeuLadenMitBearbeitungsstand();

            EmissionszeilenSetzen();
            KernwerteSpiegeln();
        }

        /// <summary>Das Kürzel der Art, zu der ein übernommener Katalogwert gehört.</summary>
        private string ErsteZeileMitWert(EmissionswertModel wert)
        {
            if (_emissionen == null || wert == null) return "";
            foreach (EmissionsZeile z in _emissionen.Zeilen)
                if (z.Art != null && z.Art.ID == wert.EmissionsartId) return z.Kuerzel;
            return "";
        }

        private void UnterdialogGeschlossen()
        {
            // Nach jedem Unterdialog wird wie bisher neu gelesen (Risiko R3).
            if (_gewaehlt != null) TraegerWaehlen(_gewaehlt.ID);
        }

        // =====================================================================
        // Bilanzjahr und Unternehmensart (BW4)
        // =====================================================================

        /// <summary>
        /// Das Jahr, für das die Katalogsätze gelesen werden: das Bilanzjahr des
        /// Projekts, ersatzweise <c>BilanzKonvention.BILANZJAHR_RUECKFALL</c>.
        /// Im Katalogkontext gilt das laufende Kalenderjahr. Wortgleich aus
        /// <c>KatalogjahrErmitteln</c> beider Blöcke — zwei verschiedene
        /// Bilanzjahre in einer Maske wären nicht erklärbar.
        /// </summary>
        private static int KatalogjahrErmitteln(int idProjekt, out string unternehmensart,
                                                out double co2PreisProjekt)
        {
            unternehmensart = DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE;
            co2PreisProjekt = 0.0;
            if (idProjekt <= 0) return DateTime.Now.Year;

            try
            {
                WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(idProjekt);
                if (p != null)
                {
                    if (!string.IsNullOrEmpty(p.Unternehmensart)) unternehmensart = p.Unternehmensart;
                    co2PreisProjekt = p.CO2Preis;
                    if (p.BilanzJahr > 0) return p.BilanzJahr;
                }
            }
            catch { }

            return BilanzKonvention.BILANZJAHR_RUECKFALL;
        }

        // =====================================================================
        // Schnellwahlsätze aus dem Gesetzeskatalog (Etappe B4, Befund A7)
        // =====================================================================

        /// <summary>
        /// Rechtfertigt die Unternehmensart den reduzierten Stromsteuersatz?
        /// Produzierendes Gewerbe (§ 2 Nr. 3 StromStG) und Land-/Forstwirtschaft
        /// sind nach § 9b entlastungsberechtigt — dieselbe Bedingung, die
        /// <c>SteuerGutschriftRechner.ProduzierendesGewerbe</c> für die Rechnung
        /// prüft, bis hin zum <c>StringComparison.Ordinal</c>.
        /// </summary>
        private bool ReduzierterSatzEmpfohlen()
        {
            return string.Equals(_unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                                 StringComparison.Ordinal)
                || string.Equals(_unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST,
                                 StringComparison.Ordinal);
        }

        /// <summary>
        /// Ein Stromsteuersatz des Bilanzjahres — wortgleich aus
        /// <c>ucStromAufschlaege.Satz</c>. Anders als beim Brennstoff gibt es
        /// hier IMMER eine Zahl: Zu jedem der beiden Sätze steht eine
        /// Rückfallebene bereit. Die Frage ist nicht „gibt es eine Zahl",
        /// sondern „woher kommt sie" — das sagt die Herkunft.
        /// </summary>
        private Schnellwahlsatz StromsteuerSatz(string schluessel, double rueckfall, bool empfohlen)
        {
            var gesetze = new GesetzKatalog();
            GesetzParameter p = null;
            try { p = gesetze.WertMitHerkunft(schluessel, _katalogJahr); }
            catch { }

            string zweck = string.Equals(schluessel, DbWerte.GESETZ_STROMST_REGELSATZ,
                                         StringComparison.Ordinal)
                ? T("PREIS_ST_ZWECK_REGELFALL", "Stromsteuer im Regelfall (§ 3 StromStG).")
                : T("PREIS_ST_ZWECK_REDUZIERT",
                    "Stromsteuer energieintensiver Unternehmen — was nach der Entlastung "
                    + "nach § 9b StromStG im Preis verbleibt.");

            string herkunft;
            double wert;
            if (p != null && p.Wert.HasValue && InCtKwhStrom(p.Wert.Value, p.Einheit, out wert))
            {
                herkunft = string.Format(T("PREIS_ST_QUELLE", "Katalog: {0} {1} (ab {2}, {3})"),
                    p.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture), p.Einheit,
                    p.JahrVon.ToString(CultureInfo.InvariantCulture), Herkunftstext(p));
            }
            else
            {
                wert = rueckfall;
                herkunft = string.Format(
                    T("PREIS_ST_QUELLE_RUECKFALL",
                      "Rückfallebene: {0} ct/kWh aus dem Programm. {1} "
                      + "Nachpflegbar über „Gesetzliche Parameter\"."),
                    Anzeige(wert),
                    string.Format(T("PREIS_ST_GRUND_KEIN_JAHR",
                        "Der Katalog führt für „{0}\" keinen Satz im Jahr {1}."),
                        schluessel, _katalogJahr.ToString(CultureInfo.InvariantCulture)));
            }

            string voll = zweck + Environment.NewLine + herkunft;
            if (empfohlen)
                voll += Environment.NewLine + string.Format(
                    T("PREIS_ST_EMPFOHLEN",
                      "Vorschlag zur Unternehmensart „{0}\" dieses Projekts. "
                      + "Eingetragen wird der Satz erst mit einem Klick."),
                    UnternehmensartAnzeige(_unternehmensart));

            return new Schnellwahlsatz(Anzeige(wert), voll, wert, empfohlen);
        }

        /// <summary>Für Strom gibt es keine Brennwertbrücke — eine Kilowattstunde
        /// Strom ist eine Kilowattstunde (wie <c>KohaerenzPruefung.Fall4Strom</c>).</summary>
        private static bool InCtKwhStrom(double wert, string einheit, out double ct)
        {
            string e = (einheit ?? "").Trim();
            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
            { ct = wert; return true; }
            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
            { ct = wert / 10.0; return true; }
            ct = 0.0;
            return false;
        }

        private static string UnternehmensartAnzeige(string wert)
        {
            if (string.Equals(wert, DbWerte.UNTERNEHMENSART_PROD_GEWERBE, StringComparison.Ordinal))
                return T("PREIS_ST_ART_PROD_GEWERBE", "produzierendes Gewerbe");
            if (string.Equals(wert, DbWerte.UNTERNEHMENSART_LAND_FORST, StringComparison.Ordinal))
                return T("PREIS_ST_ART_LAND_FORST", "Land- und Forstwirtschaft");
            return T("PREIS_ST_ART_KEIN_PROD_GEWERBE", "kein produzierendes Gewerbe");
        }

        /// <summary>
        /// Ein Energiesteuersatz — wortgleich aus
        /// <c>ucBrennstoffBestandteile.Satz</c>: Katalogschlüssel → Jahressatz →
        /// ct/kWh. Jede Stufe kann leer ausgehen, und dann sagt die Herkunft
        /// welche; eine geratene Zahl gibt es hier nicht (L3).
        /// </summary>
        private Schnellwahlsatz EnergiesteuerSatz(string schluessel, string muster)
        {
            string leer = T("BB_BTN_KEIN_SATZ", "—");

            if (string.IsNullOrEmpty(schluessel))
                return new Schnellwahlsatz(string.Format(muster, leer),
                    T("BB_GRUND_KEIN_SCHLUESSEL",
                      "Diesem Energieträger ist im Katalog kein Energiesteuersatz zugeordnet."),
                    null);

            GesetzParameter p = null;
            try { p = new GesetzKatalog().WertMitHerkunft(schluessel, _katalogJahr); }
            catch { }

            if (p == null || !p.Wert.HasValue)
                return new Schnellwahlsatz(string.Format(muster, leer),
                    string.Format(T("BB_GRUND_KEIN_JAHR",
                        "Der Katalog führt für {0} keinen Satz im Jahr {1}."),
                        schluessel, _katalogJahr), null);

            string grund;
            double? ct = InCtKwhBrennstoff(p.Wert.Value, p.Einheit, out grund);
            if (!ct.HasValue)
                return new Schnellwahlsatz(string.Format(muster, leer), grund, null);

            return new Schnellwahlsatz(
                string.Format(muster, ct.Value.ToString("0.####", CultureInfo.CurrentCulture)),
                string.Format(T("BB_QUELLE", "{0} {1} (ab {2}, {3})"),
                    p.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture),
                    p.Einheit, p.JahrVon, Herkunftstext(p)),
                ct, false, SatzInAnzeigeeinheit(ct.Value));
        }

        /// <summary>Gramm/Kilowattstunde × Euro/Tonne → Cent/Kilowattstunde.</summary>
        private const double G_KWH_MAL_EUR_T_JE_CT_KWH = 10000.0;

        /// <summary>Gigajoule je Megawattstunde.</summary>
        private const double GJ_JE_MWH = 3.6;

        /// <summary>
        /// Der CO₂-Anteil nach BEHG — wortgleich aus
        /// <c>ucBrennstoffBestandteile.SatzCo2</c>: Preis [€/t] ×
        /// Emissionsfaktor [g/kWh]. Der Preis folgt derselben Vorrangregel wie
        /// der Rechenweg (Projektwert vor Katalogpfad), der Faktor ist das reine
        /// CO₂ und heizwertbezogen.
        /// </summary>
        private Schnellwahlsatz Co2Satz()
        {
            string leer = T("BB_BTN_KEIN_SATZ", "—");
            string muster = T("BB_BTN_CO2", "BEHG: {0}");

            double preis = _co2PreisProjekt;
            string herkunftPreis;
            if (preis > 0.0)
            {
                herkunftPreis = string.Format(T("BB_QUELLE_CO2_PROJEKT", "{0} €/t (Projektwert)"),
                    preis.ToString("0.##", CultureInfo.CurrentCulture));
            }
            else
            {
                GesetzParameter g = null;
                try { g = new GesetzKatalog().WertMitHerkunft(DbWerte.GESETZ_CO2_PREIS_NEHS, _katalogJahr); }
                catch { }
                if (g == null || !g.Wert.HasValue)
                    return new Schnellwahlsatz(string.Format(muster, leer),
                        string.Format(T("BB_GRUND_KEIN_CO2_PREIS",
                            "Der Katalog führt für das Jahr {0} keinen CO₂-Preis."), _katalogJahr),
                        null);
                preis = g.Wert.Value;
                herkunftPreis = string.Format(T("BB_QUELLE_CO2_KATALOG", "{0} €/t (ab {1}, {2})"),
                    preis.ToString("0.##", CultureInfo.CurrentCulture), g.JahrVon, Herkunftstext(g));
            }

            double? ef = null;
            try { ef = EmissionsFaktorLader.Lade(_projektId, _gewaehlt.ID).Co2GKwh; }
            catch { }

            if (!ef.HasValue || ef.Value <= 0.0)
                return new Schnellwahlsatz(string.Format(muster, leer),
                    T("BB_GRUND_KEIN_EF",
                      "Für diesen Energieträger ist kein CO₂-Faktor größer null gepflegt."),
                    null);

            double ct = ef.Value * preis / G_KWH_MAL_EUR_T_JE_CT_KWH;
            return new Schnellwahlsatz(
                string.Format(muster, ct.ToString("0.####", CultureInfo.CurrentCulture)),
                string.Format(T("BB_QUELLE_CO2", "{0} × {1} g/kWh"),
                    herkunftPreis, ef.Value.ToString("0.##", CultureInfo.CurrentCulture)),
                ct, false, SatzInAnzeigeeinheit(ct));
        }

        /// <summary>
        /// Derselbe Satz in der ABRECHNUNGSEINHEIT („0,0638 €/m³") — was in der
        /// Schnellwahl-Überlagerung neben der Herkunft steht. Ohne Heizwert gibt
        /// es keine zweite Einheit; dann bleibt die Zeile leer, denn der Satz
        /// steht ohnehin schon in ct/kWh auf dem Knopf.
        /// </summary>
        private string SatzInAnzeigeeinheit(double ctKwh)
        {
            if (_gewaehlt == null || !_gewaehlt.HasHi || _baseHi <= 0.0) return "";
            return AnteilText(ctKwh) + " "
                   + EnergietraegerPreiskarte.AnteilEinheit(_abrechnungseinheit, _baseHi);
        }

        /// <summary>
        /// Die leise Zeile unter der Energiesteuer: der KATALOGSATZ in seiner
        /// eigenen Einheit, mit dem Hinweis, dass er brennwertbezogen gilt
        /// („5,50 €/MWh (Hs)"). Leer, solange der Katalog nichts hergibt — eine
        /// Herleitung ohne Grundlage wäre eine Behauptung (L3).
        /// </summary>
        private string HerleitungEnergiesteuer()
        {
            if (_gewaehlt == null) return "";
            string schluessel = WirtschaftlichkeitCtrl.EnergiesteuerSchluessel(_idBrennstoff, false);
            if (string.IsNullOrEmpty(schluessel)) return "";

            GesetzParameter g = null;
            try { g = new GesetzKatalog().WertMitHerkunft(schluessel, _katalogJahr); }
            catch { }
            if (g == null || !g.Wert.HasValue) return "";

            return string.Format(CultureInfo.CurrentCulture,
                T("BB_HERLEITUNG_STEUER", "{0} {1} (Hs)"),
                g.Wert.Value.ToString("0.##", CultureInfo.CurrentCulture), g.Einheit);
        }

        /// <summary>
        /// Die leise Zeile unter dem BEHG-Anteil: CO₂-Preis × CO₂-Menge JE
        /// ABRECHNUNGSEINHEIT („65 €/t × 2,109 kg/m³"). Die Menge fällt aus dem
        /// Emissionsfaktor und dem Heizwert: g/kWh × kWh/Einheit ÷ 1000 = kg/Einheit.
        /// </summary>
        private string HerleitungCo2()
        {
            if (_gewaehlt == null || _baseHi <= 0.0) return "";
            string einheit = (_abrechnungseinheit ?? "").Trim();
            if (einheit.Length == 0) return "";

            double preis = _co2PreisProjekt;
            if (preis <= 0.0)
            {
                GesetzParameter g = null;
                try { g = new GesetzKatalog().WertMitHerkunft(DbWerte.GESETZ_CO2_PREIS_NEHS, _katalogJahr); }
                catch { }
                if (g == null || !g.Wert.HasValue) return "";
                preis = g.Wert.Value;
            }

            double? ef = null;
            try { ef = EmissionsFaktorLader.Lade(_projektId, _gewaehlt.ID).Co2GKwh; }
            catch { }
            if (!ef.HasValue || ef.Value <= 0.0) return "";

            // Die Masse rechnet der Kern (EnergietraegerPreiskarte) — hier steht
            // kein Faktor 1000 (Einheitenregel W8-O-5c).
            double? kgJeEinheit = EnergietraegerPreiskarte.Co2MasseJeEinheit(ef.Value, _baseHi);
            if (!kgJeEinheit.HasValue) return "";

            return string.Format(CultureInfo.CurrentCulture,
                T("BB_HERLEITUNG_CO2", "{0} €/t × {1} kg/{2}"),
                preis.ToString("0.##", CultureInfo.CurrentCulture),
                kgJeEinheit.Value.ToString("N3", CultureInfo.CurrentCulture), einheit);
        }

        /// <summary>
        /// Die Einheitenkette des Konzepts § 6.2 — wortgleich aus
        /// <c>ucBrennstoffBestandteile.InCtKwh</c>. EUR/MWh ist brennwertbezogen
        /// und wird mit Hs/Hi auf den heizwertbezogenen Arbeitspreis gebracht;
        /// fehlt der Brennwert, bleibt der Faktor 1 (konservativ).
        /// </summary>
        private double? InCtKwhBrennstoff(double wert, string einheit, out string grund)
        {
            grund = "";
            string e = (einheit ?? "").Trim();

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_CT_KWH, StringComparison.OrdinalIgnoreCase))
                return wert;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_MWH, StringComparison.OrdinalIgnoreCase))
            {
                double faktor = (_baseHi > 0.0 && _baseHs > 0.0) ? _baseHs / _baseHi : 1.0;
                return wert / 10.0 * faktor;
            }

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_GJ, StringComparison.OrdinalIgnoreCase))
                return wert * GJ_JE_MWH / 10.0;

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000L, StringComparison.OrdinalIgnoreCase))
                return JeTausend(wert, "l", out grund);

            if (string.Equals(e, DbWerte.GESETZ_EINHEIT_EUR_1000KG, StringComparison.OrdinalIgnoreCase))
                return JeTausend(wert, "kg", out grund);

            grund = string.Format(T("BB_GRUND_EINHEIT_UNBEKANNT",
                "Die Katalogeinheit „{0}\" lässt sich nicht in ct/kWh umrechnen."), e);
            return null;
        }

        /// <summary>
        /// Satz je 1.000 Abrechnungseinheiten → ct/kWh. Die Brücke Liter ↔
        /// Kilogramm bräuchte die Dichte, und <c>energy_carrier.density</c> ist
        /// im gesamten Bestand leer — lieber kein Wert als eine geratene Dichte.
        /// </summary>
        private double? JeTausend(double wert, string erwartet, out string grund)
        {
            grund = "";

            if (!string.Equals(_abrechnungseinheit, erwartet, StringComparison.OrdinalIgnoreCase))
            {
                grund = string.Format(T("BB_GRUND_EINHEIT",
                    "Der Satz gilt je 1.000 {0}; dieser Träger rechnet je {1}. "
                    + "Ohne gepflegte Dichte ist die Umrechnung nicht belegbar."),
                    erwartet, string.IsNullOrEmpty(_abrechnungseinheit) ? "?" : _abrechnungseinheit);
                return null;
            }

            if (_baseHi <= 0.0)
            {
                grund = T("BB_GRUND_HEIZWERT",
                    "Ohne Heizwert lässt sich der Satz nicht in ct/kWh umrechnen.");
                return null;
            }

            return wert / (10.0 * _baseHi);
        }

        private static string Herkunftstext(GesetzParameter p)
        {
            string q = (p.Quelle ?? "").Trim();
            string st = (p.Status ?? "").Trim();
            if (q.Length == 0) return st;
            if (st.Length == 0) return q;
            return q + ", " + st;
        }

        /// <summary>
        /// Die Statuszeilen der beiden Einstiegskacheln — wortgleich aus
        /// <c>Form_Energietraeger.AktualisiereKarten</c>.
        /// </summary>
        private void KartenStatus(EnergietraegerAnsicht a)
        {
            if (a.MitKostenprofil)
            {
                try
                {
                    var vorhandene = new KostenprofilCtrl().ReadAllByProjekt(_projektId);
                    a.KarteProfilStatus = vorhandene.Count == 0
                        ? T("KPROF_STATUS_KEIN_PROFIL", "Noch kein Profil hinterlegt.")
                        : vorhandene[0].Bezeichner;
                }
                catch { a.KarteProfilStatus = "—"; }
            }

            try
            {
                var reihen = new PreisreiheCtrl().ReadVerfuegbare(_projektId);
                if (reihen.Count == 0)
                    a.KarteSpotStatus = T("KDLG_ET_SPOT_KEINE", "Noch keine Preisreihe vorhanden.");
                else
                {
                    int min = int.MaxValue, max = int.MinValue;
                    foreach (PreisreiheModel m in reihen)
                    {
                        if (m.Jahr < min) min = m.Jahr;
                        if (m.Jahr > max) max = m.Jahr;
                    }
                    a.KarteSpotStatus = string.Format(CultureInfo.CurrentCulture,
                        T("KDLG_ET_SPOT_STATUS", "{0} Reihe(n), Jahre {1}–{2}"),
                        reihen.Count, min, max);
                }
            }
            catch { a.KarteSpotStatus = "—"; }
        }

        /// <summary>Statuszeile der Saisonreihe (FK6a) — wortgleich aus
        /// <c>ucFuelSettings.ZeigeReihenStatus</c>.</summary>
        private string ReihenStatus()
        {
            if (_gewaehlt == null) return "";
            try
            {
                PreisreiheModel r = new PreisreiheCtrl().ReadTraegerReihe(_projektId, _gewaehlt.ID);
                return r == null ? "" : string.Format(CultureInfo.CurrentCulture,
                    T("KDLG_LP_REIHE_STATUS", "Saisonreihe {0} ({1}) — gilt vor dem Satz."),
                    r.Jahr,
                    r.IstStamm ? T("KDLG_LPR_EBENE_STAMM", "Stammreihe (Katalog)")
                               : T("KDLG_LPR_EBENE_PROJEKT", "Projektreihe"));
            }
            catch { return ""; }
        }

        // =====================================================================
        // Texte der Trägerkarte und der beiden Preisblöcke
        // =====================================================================

        /// <summary>
        /// Die Texte der Trägerkarte. Sie stehen als Satz, weil die Komponente
        /// sie an einen verschachtelten Baustein weiterreicht — einzeln
        /// durchgereicht wären es dreißig Parameter auf zwei Ebenen.
        /// </summary>
        private static IReadOnlyDictionary<string, object> KarteTexte()
        {
            return new Dictionary<string, object>
            {
                ["TitelBlockPreis"] = T("ETV_BLOCK_PREIS", "Preis und Heizwert"),
                ["TitelBlockEmissionen"] = T("ETV_BLOCK_EMISSIONEN",
                    "Emissionen — Anzeige folgt der Bilanzierungsvorgabe des Projekts"),
                ["TitelBlockEinheiten"] = T("ETV_BLOCK_EINHEITEN", "Einheiten und Umrechnung"),
                ["HinweisRegeln"] = T("ETV_REGELN_HINWEIS",
                    "Diese Regeln prüfen die Einheitenkette; gerechnet wird mit Heizwert "
                    + "und Brennwert."),
                ["TitelHistorie"] = T("ETV_TITEL_HISTORIE", "Preishistorie"),
                ["LabelPreisbasis"] = T("ETV_LBL_PREISBASIS", "Preisbasis"),
                ["LabelBasiseinheit"] = T("ETV_LBL_BASISEINHEIT", "Basiseinheit:"),
                ["LabelArbeitspreis"] = T("ETV_LBL_ARBEITSPREIS", "Arbeitspreis"),
                ["LabelLeistungspreis"] = T("ETV_LBL_LEISTUNGSPREIS", "Leistungspreis"),
                ["LabelGrundpreis"] = T("ETV_LBL_GRUNDPREIS", "Grundpreis"),
                ["LabelHeizwert"] = T("ETV_LBL_HEIZWERT", "Heizwert"),
                ["LabelBrennwert"] = T("ETV_LBL_BRENNWERT", "Brennwert"),
                ["LabelFormel"] = T("ETV_LBL_FORMEL", "Formel:"),
                ["ModusJahrText"] = T("KDLG_LP_MODUS_JAHR", "Jahresleistungspreis"),
                ["ModusMonatText"] = T("KDLG_LP_MODUS_MONAT", "Monatsleistungspreis"),
                ["SaisonText"] = T("KDLG_LP_SAISON", "Saisonale Sätze…"),
                ["TitelStaffel"] = T("ETV_STAFFEL_TITEL",
                    "Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)"),
                ["LabelStaffelGrenze"] = T("ETV_STAFFEL_GRENZE", "Staffelgrenze"),
                ["LabelStaffelPreis1"] = T("ETV_STAFFEL_PREIS1", "Preis bis zur Grenze"),
                ["LabelStaffelPreis2"] = T("ETV_STAFFEL_PREIS2", "Preis über der Grenze"),
                ["HinweisStaffel"] = T("ETV_STAFFEL_HINWEIS",
                    "Bemessen an der Viertelstundenspitze des Netzbezugs im Jahr: bis zur Grenze "
                    + "gilt der erste, darüber der zweite Preis. Eine gepflegte Staffel ersetzt "
                    + "Leistungspreis und saisonale Sätze dieses Stromträgers; leere Preise heißen "
                    + "„keine Staffel“."),
                ["SpalteName"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_NAME,
                ["SpalteVon"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_VON,
                ["SpalteNach"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_NACH,
                ["SpalteFaktor"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_FAKTOR,
                ["SpalteAktiv"] = MyResource.Resource.KOSTEN_UMRECHNUNG_SPALTE_AKTIV,
                ["RegelNeuText"] = MyResource.Resource.KOSTEN_UMRECHNUNG_NEU,
                ["LabelModus"] = T("KDLG_EM_MODUS", "Bilanzierungsmethode"),
                ["ModusCo2Text"] = T("KDLG_EM_MODUS_CO2",
                    "CO₂ direkt — reale Bilanz, heizwertbezogen"),
                ["ModusCo2eText"] = T("KDLG_EM_MODUS_CO2E", "CO₂-Äquivalent (GWP₁₀₀)"),
                ["SpalteArt"] = T("KDLG_EM_SP_ART", "Art"),
                ["SpalteWert"] = T("KDLG_EM_SP_WERT", "Wert"),
                ["SpalteEinheit"] = T("KDLG_EM_SP_EINHEIT", "Einheit"),
                ["SpalteHerkunft"] = T("KDLG_EM_SP_HERKUNFT", "Quelle"),
                ["KatalogZeileText"] = T("KDLG_EM_KATALOG", "Katalog…"),
                ["KatalogVerwaltenText"] = T("KDLG_EM_VERWALTEN",
                    "Emissionsarten & Katalog verwalten…"),
                ["KeinKatalogText"] = T("KDLG_EM_KEIN_KATALOG",
                    "Der Emissionsarten-Katalog ist auf dieser Datenbank nicht verfügbar "
                    + "(Migrationsschritt 57 fehlt). Es gelten die drei Bestandsfelder."),
                ["LabelCo2"] = T("ETV_LBL_CO2", "CO2  [g/kWh]"),
                ["LabelSo2"] = T("ETV_LBL_SO2", "SO2  [g/kWh]"),
                ["LabelNox"] = T("ETV_LBL_NOX", "NOx  [g/kWh]"),
                ["LabelGueltigAb"] = T("ETV_LBL_GUELTIG_AB", "Gültig ab"),
                ["SpeichernText"] = T("ADM_BTN_SPEICHERN", "Speichern"),
                ["SpalteGueltigAb"] = T("ETV_SP_GUELTIG_AB", "Gültig ab"),
                ["SpalteHeizwert"] = T("ETV_SP_HEIZWERT", "Heizwert"),
                ["SpalteHeizwertVorlage"] = T("ETV_SP_HEIZWERT_EINHEIT", "Heizwert [{0}]"),
                ["KatalogUebernehmenText"] = T("ETV_BTN_KATALOGWERTE", "Katalogwerte übernehmen"),
                ["SpalteBasisEinheit"] = T("ETV_SP_BASISEINHEIT", "Basis Einheit"),
                ["SpalteArbeitspreis"] = T("ETV_SP_ARBEITSPREIS", "Arbeitspreis"),
                ["SpalteGrundpreis"] = T("ETV_SP_GRUNDPREIS", "Grundpreis [€/a]"),
                ["SpalteLeistungspreis"] = T("ETV_SP_LEISTUNGSPREIS", "Leistungspreis"),
                ["HistorieLoeschenText"] = T("ETV_HISTORIE_LOESCHEN", "Löschen"),
                ["HistorieLoeschTitel"] = T("ETV_HISTORIE_LOESCH_TITEL", "Preisstand löschen"),
                ["VorlageHistorieLoeschen"] = T("ETV_HISTORIE_LOESCHFRAGE",
                    "Preisstand vom {0} löschen?")
            };
        }

        /// <summary>Die Texte der Strompreis-Details (SP-E-2/SP-E-3).</summary>
        private static IReadOnlyDictionary<string, object> ZerlegungTexte()
        {
            return new Dictionary<string, object>
            {
                ["TitelDetails"] = MyResource.Resource.PREIS_GRUPPE_DETAILS,
                ["GruppeBeschaffung"] = MyResource.Resource.PREIS_GRUPPE_BESCHAFFUNG,
                ["GruppeNetz"] = MyResource.Resource.PREIS_GRUPPE_NETZ,
                ["GruppeSteuern"] = MyResource.Resource.PREIS_GRUPPE_STEUERN,
                ["LabelBeschaffung"] = MyResource.Resource.PREIS_KOMP_BESCHAFFUNG,
                ["LabelVertrieb"] = MyResource.Resource.PREIS_KOMP_VERTRIEB,
                ["LabelNetzentgelt"] = MyResource.Resource.PREIS_KOMP_NETZENTGELT,
                ["LabelStromsteuer"] = MyResource.Resource.PREIS_KOMP_STROMSTEUER,
                ["LabelKonzession"] = MyResource.Resource.PREIS_KOMP_KONZESSION,
                ["LabelUmlagen"] = MyResource.Resource.PREIS_KOMP_UMLAGEN,
                ["LabelUmlagenEinzeln"] = MyResource.Resource.PREIS_UMLAGEN_EINZELN,
                ["LabelUmlageKwkg"] = MyResource.Resource.PREIS_KOMP_KWKG,
                ["LabelUmlageOffshore"] = MyResource.Resource.PREIS_KOMP_OFFSHORE,
                ["LabelUmlageStromNev"] = MyResource.Resource.PREIS_KOMP_STROMNEV19,
                ["LabelArbeitspreis"] = MyResource.Resource.PREIS_LABEL_ARBEITSPREIS,
                ["InArbeitspreisText"] = MyResource.Resource.PREIS_BTN_IN_ARBEITSPREIS,
                ["VorlageRest"] = MyResource.Resource.PREIS_BTN_REST,
                ["Einheit"] = DbWerte.PREISREIHE_EINHEIT_CT_KWH
            };
        }

        /// <summary>Die Texte der Preiszerlegung — wortgleich aus
        /// <c>ucBrennstoffBestandteile.TexteSetzen</c>.</summary>
        private static IReadOnlyDictionary<string, object> BestandteilTexte()
        {
            return new Dictionary<string, object>
            {
                ["TitelBestandteile"] = T("BB_GRUPPE_BESTANDTEILE",
                    "Preisbestandteile — Transparenz, ohne Preiswirkung"),
                ["LabelEnergiesteuer"] = T("BB_KOMP_ENERGIESTEUER", "Energiesteuer"),
                ["LabelCo2"] = T("BB_KOMP_CO2", "CO₂-Bestandteil (BEHG)"),
                ["LabelNetzentgelt"] = T("BB_KOMP_NETZENTGELT", "Netz- und Messentgelt"),
                ["LabelVertrieb"] = T("BB_KOMP_VERTRIEB", "Beschaffung und Vertrieb"),
                ["LabelArbeitspreis"] = T("BB_LABEL_ARBEITSPREIS", "Arbeitspreis (Trägerdialog)"),
                ["InArbeitspreisText"] = T("BB_BTN_IN_ARBEITSPREIS", "In Arbeitspreis übernehmen"),
                ["SchnellwahlText"] = T("BB_BTN_SCHNELLWAHL", "Schnellwahl aus Katalog…"),
                ["SchnellwahlTitel"] = T("BB_SCHNELLWAHL_TITEL", "Schnellwahl aus Katalog"),
                ["RestKnopfText"] = T("BB_BTN_REST", "Rest übernehmen"),
                ["VorlageRest"] = T("BB_REST_VORSCHLAG", "Rest: {0} {1}"),
                ["Einheit"] = DbWerte.PREISREIHE_EINHEIT_CT_KWH
            };
        }

        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
