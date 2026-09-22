using System;
using System.Collections.Generic;
using System.Windows.Forms;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Reiters „Berichte &amp; Kosten" (iU9-W5.6) —
    /// Nachfolge von <c>Views/BerichteKosten/UcBerichteKosten.cs</c> (810 Z.).
    ///
    /// <para><b>Sie ist die NICHT-MODALE Hülle</b>
    /// (<see cref="BlazorSeite{T}"/>) und sitzt in
    /// <c>Form_Start.tabPage6</c>. Eine WebView trägt alle vier Seiten
    /// (Risiko R5); umgeschaltet wird in der Komponente.</para>
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
    /// </summary>
    internal sealed class BerichteKostenHuelle
    {
        private readonly SeitenZustand _zustand = new SeitenZustand();
        private readonly Func<Form> _besitzer;

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

        internal BerichteKostenHuelle(Func<Form> besitzer)
        {
            _besitzer = besitzer;
        }

        /// <summary>Der Parametersatz der Reiterkomponente.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                [SeitenZustand.PARAMETER] = _zustand,
                ["SeitenGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(SeitenGaben),
                ["Kopf"] = new Func<string, string>(Kopf),
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
                    _uebersicht = new UebersichtSeiteGaben(_besitzer, _stand) { Vergleich = _vergleich };
                    _uebersicht.StammGewechselt += StammWechsel;
                    _uebersicht.ProjektMarkiert += Markierung;
                }
                return _uebersicht;
            }
        }

        private KostenSeiteGaben Kosten
        {
            get
            {
                if (_kosten == null) _kosten = new KostenSeiteGaben { Vergleich = _vergleich };
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
                        _wirtschaft = new WirtschaftlichkeitSeiteGaben(
                            _stand.IdStamm, _stand.StammName)
                        {
                            Vergleich = _vergleich
                        };
                    return _wirtschaft.Gaben();

                case BerichteKostenSeite.SEITE_BERICHT:
                    if (_stand.IdStamm <= 0) return null;
                    GruppenseitenPruefen();
                    if (_bericht == null)
                        _bericht = new BerichtSeiteGaben(_stand.IdStamm, _stand.StammName)
                        {
                            // KONZEPT § 2.15 (VG-Q4): Der Bericht folgt derselben Sicht
                            // wie die Ergebnisansicht - dieselbe Sitzungswahl, dieselbe
                            // Instanz.
                            Vergleich = _vergleich
                        };
                    return _bericht.Gaben();
            }
            return null;
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

        /// <summary>Die Kopfzeile der Seite — Titel und Stammname.</summary>
        private string Kopf(string seite)
        {
            string kopf = KopfText(seite);
            return _stand.IdStamm > 0 && !string.IsNullOrEmpty(kopf)
                ? kopf + "  ·  " + _stand.StammName
                : kopf;
        }

        private static string KopfText(string seite)
        {
            switch (seite)
            {
                case BerichteKostenSeite.SEITE_UEBERSICHT: return MyResource.Resource.BK_KOPF_UEBERSICHT;
                case BerichteKostenSeite.SEITE_KOSTEN: return MyResource.Resource.BK_KOPF_KOSTEN;
                case BerichteKostenSeite.SEITE_WIRTSCHAFT: return MyResource.Resource.BK_KOPF_WIRTSCHAFT;
                case BerichteKostenSeite.SEITE_BERICHT: return MyResource.Resource.BK_KOPF_BERICHT;
                default: return "";
            }
        }
    }
}
