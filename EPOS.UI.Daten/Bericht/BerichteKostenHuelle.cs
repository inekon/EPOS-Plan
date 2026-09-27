using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die PLATTFORMFREIE Hülle des Reiters „Berichte &amp; Kosten" (iU9-W5.6) —
    /// Nachfolge von <c>Views/BerichteKosten/UcBerichteKosten.cs</c> (810 Z.).
    ///
    /// <para><b>Seit Etappe E3, Schritt 8 liegt sie in <c>EPOS.UI.Daten</c></b>
    /// und baut die Parametersätze ihrer vier Seiten allein aus
    /// Kern-Controllern. Unter Windows sitzt sie in einer
    /// <c>BlazorSeite</c> in <c>Form_Start.tabPage6</c> und speist zugleich die
    /// Ansicht der <c>AppWurzel</c>; auf iOS liefert sie dieselben Sätze über
    /// <c>IProjektQuelle.BerichteKostenGaben</c>. Eine WebView trägt alle vier
    /// Seiten (Risiko R5); umgeschaltet wird in der Komponente.</para>
    ///
    /// <para><b>Der geteilte Zustand.</b> Was der Vorläufer über vier Felder und
    /// zwei Ereignisse der Übersichtsseite hielt, hält die plattformfreie
    /// <see cref="Berichtsgruppe"/>: Stammprojekt und markierte Version stehen
    /// EINMAL da, und die Hülle reicht dieselbe Instanz an die Übersicht weiter.
    /// Die Kostenseite folgt der Markierung; Wirtschaftlichkeit und Bericht hängen
    /// an der Vergleichsgruppe und werden bei einem Stammwechsel VERWORFEN — sie
    /// entstehen beim nächsten Aufruf neu.</para>
    ///
    /// <para><b>Der Projektwechsel</b> läuft über
    /// <see cref="SeitenZustand"/>: <c>Form_Start</c> ruft
    /// <see cref="SetzeProjekt"/>, die Komponente holt ihre Parametersätze neu
    /// — ohne die WebView neu zu bauen.</para>
    ///
    /// <para><b>Die Kurzstände der Reiterzeile</b> (Konzept Navigation Berichte &amp; Kosten,
    /// Variante A, A2/A3): Jeder der vier Reiter trägt eine Statuszeile — Versionen der Gruppe,
    /// Befunde der Kosten, bester Kapitalwert, zuletzt erstellter Bericht. Die Hülle nennt nur,
    /// was sie OHNE RECHNUNG weiß: Übersicht und Kosten, sobald ihre Seite geladen hat (ihr
    /// Laden zählt mit), Wirtschaftlichkeit aus den GESPEICHERTEN Ergebnissen und Bericht aus
    /// der Konfiguration der Gruppe — je eine leichte Lesung, zwischengespeichert je
    /// Stammprojekt. Ändert sich ein Kurzstand, meldet sie es über
    /// <see cref="SeitenZustand.KurzstandMelden"/>; die Komponente zeichnet dann nur neu.</para>
    /// </summary>
    internal sealed class BerichteKostenHuelle
    {
        private readonly SeitenZustand _zustand = new SeitenZustand();

        /// <summary>
        /// Die zwei Wege, die nur die SCHALE kennt (E3/8): der Variantendialog
        /// als zweites Fenster und das Umbenennen mit Nachlauf der Startseite.
        /// Beide sind <c>null</c>, wo es sie nicht gibt — dann zeichnet die
        /// Übersicht den Anlegeknopf nicht und benennt über den plattformfreien
        /// Weg um.
        /// </summary>
        private readonly Func<int, string, Task<bool>> _varianteAnlegen;
        private readonly Func<int, string, string, string> _umbenennen;

        private UebersichtSeiteGaben _uebersicht;
        private KostenSeiteGaben _kosten;
        private WirtschaftlichkeitSeiteGaben _wirtschaft;
        private BerichtSeiteGaben _bericht;

        /// <summary>
        /// DER EINE Gruppenstand der vier Seiten: das Stammprojekt und die markierte
        /// Version. Er gehört dieser Hülle und wird der Übersicht hineingereicht —
        /// bis zum Anwenderbefund vom 16.09.2026 führten beide ihre eigenen Felder,
        /// und die Kostenseite las am Ende eine andere Antwort, als die Zeile davor
        /// gesetzt hatte.
        /// </summary>
        private readonly Berichtsgruppe _stand = new Berichtsgruppe();

        /// <summary>
        /// Das Stammprojekt, für das <see cref="_wirtschaft"/> und
        /// <see cref="_bericht"/> gebaut sind — die beiden hängen fest an ihrer
        /// Vergleichsgruppe und entstehen beim Wechsel neu.
        /// </summary>
        private int _seitenStamm = Berichtsgruppe.KEINS;

        /// <summary>
        /// DIE EINE Vergleichswahl der Seiten Übersicht, Kosten und Wirtschaftlichkeit
        /// (Anwenderwunsch 08.09.2026, W5‑B‑5): welche Versionen der Gruppe nebeneinander
        /// stehen. Sie lebt so lange wie diese Hülle; die Vorgabe ist „alle".
        /// </summary>
        private readonly Vergleichsauswahl _vergleich = new Vergleichsauswahl();

        /// <summary>
        /// Baut die Hülle. Beide Wege sind freiwillig: Wo eine Schale sie nicht
        /// stellt, fällt der Knopf weg bzw. der plattformfreie Weg ein.
        /// </summary>
        internal BerichteKostenHuelle(Func<int, string, Task<bool>> varianteAnlegen = null,
                                      Func<int, string, string, string> umbenennen = null)
        {
            _varianteAnlegen = varianteAnlegen;
            _umbenennen = umbenennen;
        }

        /// <summary>Der Parametersatz der Reiterkomponente.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                [SeitenZustand.PARAMETER] = _zustand,
                ["SeitenGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(SeitenGaben),
                ["Stamm"] = new Func<string>(Stammname),
                ["StammBeschriftung"] = R.BK_LBL_STAMM,
                ["Status"] = new Func<string, Reiterstatus>(Kurzstand),
                ["Seitenwunsch"] = new Func<string>(Seitenwunsch),

                ["NavUebersicht"] = MyResource.Resource.BK_NAV_UEBERSICHT,
                ["NavKosten"] = MyResource.Resource.BK_NAV_KOSTEN,
                ["NavWirtschaft"] = MyResource.Resource.BK_NAV_WIRTSCHAFT,
                ["NavBericht"] = MyResource.Resource.BK_NAV_BERICHT,
                ["NavigationBezeichnung"] = MyResource.Resource.BK_KOPF_UEBERSICHT,
                ["KeinStammText"] = MyResource.Resource.BK_MSG_KEIN_STAMM,
                ["HilfeSchluessel"] = "UcBerichteKosten.btn_Help",

                // Der Rückwegknopf erscheint nur, wo ein „Geschlossen"-Rückruf
                // gesetzt ist — also in der ANSICHT der AppWurzel
                // (Anwenderentscheid W16c-E-3), nicht im sechsten Reiterblatt
                // der Startseite. Der Text steht trotzdem immer bereit; ihn nur
                // im einen Fall zu setzen, wäre eine zweite Gabenfassung.
                ["ZurueckText"] = MyResource.Resource.BK_BTN_ZURUECK
            };
        }

        /// <summary>Der geteilte Zustand — die Seitenhülle reicht ihn hinein.</summary>
        internal SeitenZustand Zustand { get { return _zustand; } }

        /// <summary>
        /// Setzt den Projektkontext (das in <c>Form_Start</c> geöffnete
        /// Projekt). Die Komponente erfährt es über das Änderungsereignis.
        /// </summary>
        internal void SetzeProjekt(int idProjekt, string projektname)
        {
            Uebersicht.SetzeAktuellesProjekt(idProjekt, projektname);

            // Nach einem Projektwechsel (auch nach einer Simulation) lesen Wirtschaftlichkeit
            // und Bericht ihren Kurzstand neu — „veraltet" kann sich geändert haben.
            _kurzWirtschaft = null;
            _kurzBericht = null;

            _zustand.ProjektSetzen(idProjekt, projektname ?? "");

            // Ein Projektwechsel ohne Wechsel der Id (Auffrischen nach dem
            // Anlegen einer Variante) muss trotzdem durchschlagen.
            _zustand.Auffrischen();
        }

        // =====================================================================
        // Die vier Seiten
        // =====================================================================

        private UebersichtSeiteGaben Uebersicht
        {
            get
            {
                if (_uebersicht == null)
                {
                    _uebersicht = new UebersichtSeiteGaben(_varianteAnlegen, _stand, _umbenennen)
                    {
                        Vergleich = _vergleich
                    };
                    _uebersicht.StammGewechselt += StammWechsel;
                    _uebersicht.ProjektMarkiert += Markierung;
                    _uebersicht.Geladen += UebersichtGeladen;
                }
                return _uebersicht;
            }
        }

        private KostenSeiteGaben Kosten
        {
            get
            {
                if (_kosten == null)
                {
                    _kosten = new KostenSeiteGaben { Vergleich = _vergleich };
                    _kosten.Geladen += KostenGeladen;
                }
                return _kosten;
            }
        }

        private IReadOnlyDictionary<string, object> SeitenGaben(string seite)
        {
            switch (seite)
            {
                case BerichteKostenSeite.SEITE_UEBERSICHT:
                    return Uebersicht.Gaben();

                case BerichteKostenSeite.SEITE_KOSTEN:
                    // EINE Entscheidung, nicht zwei (Anwenderbefund 16.09.2026):
                    // Die Kostenseite folgt der markierten Version, und ohne
                    // Markierung steht das Stammprojekt der Gruppe. Bis dahin setzte
                    // ein SichereMarkierung() zuerst das Stammprojekt, und die
                    // naechste Zeile ueberschrieb es unbesehen mit der leeren
                    // Markierung - nach dem Rueckwechsel von einer Variante auf ihr
                    // Stammprojekt stand die Seite mit "Kein Projekt gewaehlt." da.
                    Kosten.SetzeGruppe(_stand.IdStamm, _stand.StammName);
                    Kosten.SetzeProjekt(_stand.KostenId, _stand.KostenName);
                    return Kosten.Gaben();

                case BerichteKostenSeite.SEITE_WIRTSCHAFT:
                    if (_stand.IdStamm <= 0) return null;
                    GruppenseitenPruefen();
                    if (_wirtschaft == null)
                    {
                        _wirtschaft = new WirtschaftlichkeitSeiteGaben(
                            _stand.IdStamm, _stand.StammName)
                        {
                            Vergleich = _vergleich,

                            // BV-E2 (Konzept Berichtsvorlagen 9.5): Die Anhang-E-Ueberlagerung nennt
                            // die Stellen der Vorlage, die die Berichtsseite derselben Gruppe gewaehlt
                            // hat - dieselbe Huelle, dieselbe Wahl.
                            AnhangEStellenLaden = () => BerichtGaben().Vorlagen.AnhangEStellenDerVorlage()
                        };
                        _wirtschaft.Geladen += WirtschaftGeladen;
                    }
                    return _wirtschaft.Gaben();

                case BerichteKostenSeite.SEITE_BERICHT:
                    if (_stand.IdStamm <= 0) return null;
                    GruppenseitenPruefen();
                    return BerichtGaben().Gaben();
            }
            return null;
        }

        /// <summary>
        /// Die Hülle der Berichtsseite — einmal je Vergleichsgruppe, entstanden beim ersten
        /// Aufruf der Seite ODER beim ersten Öffnen der Anhang-E-Überlagerung der Ergebnisseite.
        /// </summary>
        private BerichtSeiteGaben BerichtGaben()
        {
            if (_bericht == null)
            {
                _bericht = new BerichtSeiteGaben(_stand.IdStamm, _stand.StammName)
                {
                    // KONZEPT § 2.15 (VG-Q4): Der Bericht folgt derselben Sicht wie die
                    // Ergebnisansicht - dieselbe Sitzungswahl, dieselbe Instanz.
                    Vergleich = _vergleich
                };
                _bericht.Erstellt += BerichtErstellt;
            }
            return _bericht;
        }

        // =====================================================================
        // Der geteilte Zustand
        // =====================================================================

        private void StammWechsel(int idStamm, string name)
        {
            // Die Meldung der Uebersicht ist der ANLASS; die Antwort steht im Stand.
            GruppenseitenPruefen();
        }

        /// <summary>
        /// Wirtschaftlichkeit und Bericht hängen FEST an ihrer Vergleichsgruppe:
        /// Steht ein anderes Stammprojekt, werden sie verworfen und entstehen beim
        /// nächsten Aufruf frisch.
        ///
        /// <para>Entschieden wird am <see cref="_stand"/>, nicht am Argument der
        /// Meldung — das Laden der Liste darf das Stammprojekt auch OHNE Meldung
        /// wechseln (der Rückfall auf den ersten Eintrag), und dann trügen die
        /// beiden Seiten weiter die alte Gruppe.</para>
        /// </summary>
        private void GruppenseitenPruefen()
        {
            if (_seitenStamm == _stand.IdStamm) return;

            _seitenStamm = _stand.IdStamm;
            _wirtschaft = null;
            _bericht = null;
        }

        private void Markierung(int idProjekt, string name)
        {
            Kosten.SetzeProjekt(idProjekt, name ?? "");
        }

        /// <summary>
        /// Der einmalige Seitenwunsch des Menüwegs
        /// (<c>Form_Start.ZeigeBerichteKosten</c>). Er gilt genau einmal;
        /// danach entscheidet wieder die Navigation der Komponente.
        /// </summary>
        private string _wunsch = "";

        /// <summary>Stellt die Seite mit diesem Schlüssel ein (Menüweg).</summary>
        internal void ZeigeSeite(string schluessel)
        {
            _wunsch = schluessel ?? "";
            _zustand.Auffrischen();
        }

        private string Seitenwunsch()
        {
            string w = _wunsch;
            _wunsch = "";
            return w;
        }

        // =====================================================================
        // Die Reiterzeile: Stammname und Kurzstände (Konzept Navigation, A2/A3)
        // =====================================================================

        /// <summary>Trenner zwischen zwei Angaben einer Statuszeile.</summary>
        private const string TRENNER = " · ";

        /// <summary>Der Name des Stammprojekts für das Ende der Reiterzeile; leer = keins.</summary>
        private string Stammname()
        {
            return _stand.IdStamm > 0 ? (_stand.StammName ?? "") : "";
        }

        /// <summary>Übersicht: das Stammprojekt des letzten Ladens, seine Versionen, davon nicht aktuell.</summary>
        private int _kurzUebersichtStamm = Berichtsgruppe.KEINS;
        private int _kurzVersionen;
        private int _kurzNichtAktuell;

        /// <summary>Kosten: das Projekt des letzten Ladens, seine Energieträger (-1 = keins), Befunde.</summary>
        private int _kurzKostenProjekt = Berichtsgruppe.KEINS;
        private int _kurzTraeger = -1;
        private int _kurzBefunde;

        /// <summary>
        /// Wirtschaftlichkeit und Bericht: je Stammprojekt EINMAL gelesen und hier gehalten;
        /// <c>null</c> = noch nicht gelesen. Ein anderes Stammprojekt verwirft beide.
        /// </summary>
        private int _kurzGruppenStamm = Berichtsgruppe.KEINS;
        private Reiterstatus _kurzWirtschaft;
        private Reiterstatus _kurzBericht;

        /// <summary>
        /// Der Kurzstand eines Reiters; <c>null</c> = keine Statuszeile. Gefragt bei jedem
        /// Zeichnen der Komponente — deshalb rechnet hier nichts: Übersicht und Kosten nennen
        /// den Stand ihres letzten Ladens, Wirtschaftlichkeit und Bericht ihre zwischengespeicherte
        /// leichte Lesung.
        /// </summary>
        internal Reiterstatus Kurzstand(string seite)
        {
            switch (seite)
            {
                case BerichteKostenSeite.SEITE_UEBERSICHT:
                    return UebersichtKurzstand();
                case BerichteKostenSeite.SEITE_KOSTEN:
                    return KostenKurzstand();
                case BerichteKostenSeite.SEITE_WIRTSCHAFT:
                    if (!GruppeBekannt()) return null;
                    if (_kurzWirtschaft == null) _kurzWirtschaft = WirtschaftLesen();
                    return _kurzWirtschaft;
                case BerichteKostenSeite.SEITE_BERICHT:
                    if (!GruppeBekannt()) return null;
                    if (_kurzBericht == null) _kurzBericht = BerichtLesen();
                    return _kurzBericht;
            }
            return null;
        }

        /// <summary>Steht ein Stammprojekt? Ein anderes als beim letzten Lesen verwirft die Zwischenstände.</summary>
        private bool GruppeBekannt()
        {
            if (_stand.IdStamm <= 0) return false;
            if (_kurzGruppenStamm != _stand.IdStamm)
            {
                _kurzGruppenStamm = _stand.IdStamm;
                _kurzWirtschaft = null;
                _kurzBericht = null;
            }
            return true;
        }

        private Reiterstatus UebersichtKurzstand()
        {
            if (_kurzUebersichtStamm <= 0 || _kurzUebersichtStamm != _stand.IdStamm) return null;

            string versionen = Anzahl(_kurzVersionen, R.BK_STATUS_VERSION, R.BK_STATUS_VERSIONEN);
            if (_kurzNichtAktuell > 0)
                return new Reiterstatus(
                    string.Format(CultureInfo.CurrentCulture, R.BK_STATUS_NICHT_AKTUELL, versionen, _kurzNichtAktuell),
                    _kurzNichtAktuell.ToString(CultureInfo.CurrentCulture), Statusstufe.Warnung);
            return new Reiterstatus(string.Format(CultureInfo.CurrentCulture, R.BK_STATUS_SIMULIERT, versionen),
                                    versionen);
        }

        private Reiterstatus KostenKurzstand()
        {
            if (_kurzTraeger < 0 || _kurzKostenProjekt <= 0 || _kurzKostenProjekt != _stand.KostenId) return null;

            string traeger = Anzahl(_kurzTraeger, R.BK_STATUS_TRAEGER_1, R.BK_STATUS_TRAEGER);
            if (_kurzBefunde > 0)
                return new Reiterstatus(
                    traeger + TRENNER + Anzahl(_kurzBefunde, R.BK_STATUS_WARNUNG, R.BK_STATUS_WARNUNGEN),
                    _kurzBefunde.ToString(CultureInfo.CurrentCulture), Statusstufe.Warnung);
            return new Reiterstatus(traeger);
        }

        /// <summary>
        /// Die leichte Lesung der Wirtschaftlichkeit: die GESPEICHERTEN Ergebnisse der Gruppe
        /// (<see cref="WirtschaftlichkeitCtrl.LadeErgebnisse"/>), die beste Variante nach derselben
        /// Regel wie Karten und Bericht (<see cref="BesteVariante.Waehle"/>) und ob die Ergebnisse
        /// noch zum Simulationsstand passen. Gerechnet wird nichts. Ein Lesefehler kostet nur die
        /// Zeile.
        /// </summary>
        private Reiterstatus WirtschaftLesen()
        {
            try
            {
                var ids = new List<int>();
                var namen = new Dictionary<int, string>();
                foreach (VariantenCtrl.VarianteInfo vi in new VariantenCtrl().LadeGruppe(_stand.IdStamm, _stand.StammName))
                {
                    ids.Add(vi.IdProjekt);
                    namen[vi.IdProjekt] = vi.IstStamm
                        ? R.BK_ART_STAMM
                        : (string.IsNullOrEmpty(vi.Variantenname) ? vi.Projektname : vi.Variantenname);
                }
                if (ids.Count == 0) ids.Add(_stand.IdStamm);

                var ctrl = new WirtschaftlichkeitCtrl();
                List<WirtschaftlichkeitErgebnis> ergebnisse = ctrl.LadeErgebnisse(ids);
                if (ergebnisse.Count == 0) return new Reiterstatus(R.BK_STATUS_NICHT_BERECHNET);

                List<int> spalten = _vergleich.Sicht.Spalten(_vergleich.Gewaehlte(ids, _stand.IdStamm));
                BesteVariante.Auswahl wahl = BesteVariante.Waehle(ergebnisse, _stand.IdStamm, spalten);
                CultureInfo kultur = BerichtTexte.Kultur;

                string text, kurz;
                if (wahl.Grund == BesteVariante.Auswahlgrund.BestesKriterium && wahl.Ergebnis?.KapitalwertDiff != null)
                {
                    WirtschaftlichkeitErgebnis beste = wahl.Ergebnis;
                    string name;
                    if (!namen.TryGetValue(beste.IdProjekt, out name) || string.IsNullOrEmpty(name)) name = beste.Anzeige;
                    kurz = Euro(beste.KapitalwertDiff.Value, kultur, true);
                    text = string.Format(CultureInfo.CurrentCulture, R.BK_STATUS_BESTE, name, kurz);
                }
                else if (wahl.Ergebnis?.Kapitalwert != null)
                {
                    kurz = Euro(wahl.Ergebnis.Kapitalwert.Value, kultur, false);
                    text = string.Format(CultureInfo.CurrentCulture, R.BK_STATUS_STAMM_KW, kurz);
                }
                else
                {
                    return new Reiterstatus(R.BK_STATUS_NICHT_BERECHNET);
                }

                // Passen die gespeicherten Ergebnisse noch zum Simulationsstand? Dieselbe Frage
                // wie die Statuszeile der Seite — je Projekt und Ergebnisstand EINMAL gestellt.
                var gefragt = new HashSet<string>();
                foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
                {
                    if (!gefragt.Add(e.IdProjekt.ToString(CultureInfo.InvariantCulture) + "/" +
                                     e.IdErgebnis.ToString(CultureInfo.InvariantCulture))) continue;
                    if (!ctrl.ErgebnisAktuell(e))
                        return new Reiterstatus(text + TRENNER + R.BK_STATUS_VERALTET, R.BK_STATUS_VERALTET,
                                                Statusstufe.Warnung);
                }
                return new Reiterstatus(text, kurz);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Die leichte Lesung des Berichts: der gemerkte Zeitpunkt der Gruppe (Etappe A3).</summary>
        private Reiterstatus BerichtLesen()
        {
            DateTime? zuletzt;
            try { zuletzt = new BerichtCtrl().ZuletztErstellt(_stand.IdStamm); }
            catch (Exception) { return null; }
            return BerichtKurzstand(zuletzt);
        }

        /// <summary>„zuletzt …" samt Kurzform; ohne Zeitpunkt „noch keiner erstellt".</summary>
        internal static Reiterstatus BerichtKurzstand(DateTime? zuletzt)
        {
            if (zuletzt == null) return new Reiterstatus(R.BK_STATUS_BERICHT_KEINER, "—");
            return new Reiterstatus(
                string.Format(CultureInfo.CurrentCulture, R.BK_STATUS_BERICHT_ZULETZT, zuletzt.Value),
                string.Format(CultureInfo.CurrentCulture, R.BK_STATUS_BERICHT_KURZ, zuletzt.Value));
        }

        /// <summary>Eine Zahl mit ihrem Wort — Einzahl oder Mehrzahl.</summary>
        private static string Anzahl(int n, string einzahl, string mehrzahl)
        {
            return string.Format(CultureInfo.CurrentCulture, n == 1 ? einzahl : mehrzahl, n);
        }

        /// <summary>Ein Eurobetrag ohne Nachkommastellen; mit Vorzeichen auch das Plus.</summary>
        private static string Euro(double wert, CultureInfo kultur, bool mitVorzeichen)
        {
            string zahl = wert.ToString("N0", kultur) + " €";
            return mitVorzeichen && wert > 0 ? "+" + zahl : zahl;
        }

        // ---- die Meldungen der Seiten ----------------------------------------

        private void UebersichtGeladen(int idStamm, int versionen, int nichtAktuell)
        {
            bool neu = idStamm != _kurzUebersichtStamm || versionen != _kurzVersionen
                       || nichtAktuell != _kurzNichtAktuell;
            _kurzUebersichtStamm = idStamm;
            _kurzVersionen = versionen;
            _kurzNichtAktuell = nichtAktuell;
            if (neu) _zustand.KurzstandMelden();
        }

        private void KostenGeladen(int idProjekt, int traeger, int befunde)
        {
            bool neu = idProjekt != _kurzKostenProjekt || traeger != _kurzTraeger || befunde != _kurzBefunde;
            _kurzKostenProjekt = idProjekt;
            _kurzTraeger = traeger;
            _kurzBefunde = befunde;
            if (neu) _zustand.KurzstandMelden();
        }

        private void WirtschaftGeladen()
        {
            // Gelesen oder gerechnet: Die Zeile liest beim nächsten Zeichnen neu.
            _kurzWirtschaft = null;
            _zustand.KurzstandMelden();
        }

        private void BerichtErstellt(DateTime zeitpunkt)
        {
            if (GruppeBekannt()) _kurzBericht = BerichtKurzstand(zeitpunkt);
            _zustand.KurzstandMelden();
        }
    }
}
