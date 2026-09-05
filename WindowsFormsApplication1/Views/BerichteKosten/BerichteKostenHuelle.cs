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
    /// <para><b>Der geteilte Zustand.</b> Was der Vorläufer über vier Felder
    /// (<c>_idStamm</c>, <c>_stammName</c>, <c>_idMarkiert</c>,
    /// <c>_nameMarkiert</c>) und zwei Ereignisse der Übersichtsseite hielt,
    /// hält diese Hülle: Die Übersicht meldet Stammwechsel und Markierung, die
    /// Kostenseite folgt der Markierung, Wirtschaftlichkeit und Bericht hängen
    /// an der Vergleichsgruppe und werden bei einem Stammwechsel VERWORFEN —
    /// sie entstehen beim nächsten Aufruf neu.</para>
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

        private int _idStamm = -1;
        private string _stammName = "";

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
            Uebersicht.SetzeAktuellesProjekt(idProjekt);
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
                    _uebersicht = new UebersichtSeiteGaben();
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
                if (_kosten == null) _kosten = new KostenSeiteGaben(_besitzer);
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
                    // Der Vorlaeufer sicherte hier die Markierung ab: Wer den
                    // Reiter betritt und ohne Umweg ueber die Uebersicht auf
                    // "Kosten" geht, bekaeme sonst -1 (SichereMarkierung).
                    SichereMarkierung();
                    Kosten.SetzeProjekt(Uebersicht.IdMarkiert, Uebersicht.NameMarkiert);
                    return Kosten.Gaben();

                case BerichteKostenSeite.SEITE_WIRTSCHAFT:
                    if (_idStamm <= 0) return null;
                    if (_wirtschaft == null)
                        _wirtschaft = new WirtschaftlichkeitSeiteGaben(_idStamm, _stammName, _besitzer);
                    return _wirtschaft.Gaben();

                case BerichteKostenSeite.SEITE_BERICHT:
                    if (_idStamm <= 0) return null;
                    if (_bericht == null)
                        _bericht = new BerichtSeiteGaben(_idStamm, _stammName);
                    return _bericht.Gaben();
            }
            return null;
        }

        /// <summary>
        /// Fängt den Fall ab, dass die Kostenseite ohne Projekt dastünde:
        /// die markierte Zeile, sonst das Stammprojekt der Gruppe.
        /// </summary>
        private void SichereMarkierung()
        {
            if (Uebersicht.IdMarkiert > 0) return;
            if (_idStamm > 0) Kosten.SetzeProjekt(_idStamm, _stammName);
        }

        // =====================================================================
        // Der geteilte Zustand
        // =====================================================================

        private void StammWechsel(int idStamm, string name)
        {
            if (idStamm == _idStamm) { _stammName = name ?? ""; return; }

            _idStamm = idStamm;
            _stammName = name ?? "";

            // Wirtschaftlichkeit und Bericht haengen fest an ihrer
            // Vergleichsgruppe: beim Stammwechsel verwerfen, damit sie beim
            // naechsten Aufruf frisch mit dem neuen Stamm entstehen.
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
            return _idStamm > 0 && !string.IsNullOrEmpty(kopf)
                ? kopf + "  ·  " + _stammName
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
