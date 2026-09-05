using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Wirtschaftlichkeitsseite (iU9-W5.3/W5.6) — Nachfolge
    /// von <c>Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs</c> (831 Z.).
    ///
    /// <para><b>Was hier liegt.</b> Laden und Rechnen über
    /// <see cref="WirtschaftlichkeitCtrl"/> und
    /// <see cref="BerichtsDatenSammler"/>, der Parameternachweis (L12/L13),
    /// die vier Kennzahl-Karten (KD6a) und die Zeilen der Vergleichstabelle
    /// (<c>WirtschaftlichkeitZeilen.Kennzahlen</c> — EINE Zeilendefinition für
    /// Seite, Word und Excel). Dazu die Parametersätze der fünf Unterdialoge.
    /// Die Komponente <see cref="WirtschaftlichkeitSeite"/> zeigt nur an.</para>
    ///
    /// <para><b>Zwei Zwischenspeicher wie im Vorläufer.</b> Der Tarif wird
    /// EINMAL je Sitzung gelesen (E7: die Beschriftung der Stromkostenzeile
    /// hängt daran, und <c>ZeigeErgebnisse</c> läuft bei jedem
    /// Szenariowechsel), die Emissionsbilanzen einmal je Datenstand (Review
    /// Phase 8 — nicht bei jedem Wechsel im Oberflächenfaden rechnen).</para>
    /// </summary>
    internal sealed class WirtschaftlichkeitSeiteGaben
    {
        private readonly int _idStamm;
        private readonly string _stammName;
        private readonly Func<Form> _besitzer;

        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();

        private List<WirtschaftlichkeitErgebnis> _ergebnisse = new List<WirtschaftlichkeitErgebnis>();
        private readonly Dictionary<int, string> _namen = new Dictionary<int, string>();
        private readonly Dictionary<int, EmissionsBilanz> _bilanzen = new Dictionary<int, EmissionsBilanz>();

        private WirtschaftlichkeitParameter _parameterCache;
        private TarifParameter _tarifCache;

        private CancellationTokenSource _cts;

        /// <summary>
        /// Die Szenarien als Nummer. Die PERSISTENZWERTE
        /// (<c>Tab_ErgebnisWirtschaftlichkeit.Szenario</c>) kennt nur diese
        /// Hülle — sie dürfen weder in die Komponente noch in eine <c>.resx</c>.
        /// </summary>
        private static readonly string[] SZENARIEN =
        {
            WirtschaftlichkeitSzenario.ERWARTET,
            WirtschaftlichkeitSzenario.BEST,
            WirtschaftlichkeitSzenario.WORST
        };

        internal WirtschaftlichkeitSeiteGaben(int idStamm, string stammName, Func<Form> besitzer)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            _besitzer = besitzer;
        }

        /// <summary>Läuft gerade eine Berechnung?</summary>
        internal bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Der Parametersatz der Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<WirtschaftlichkeitStand>(Laden),
                ["Anzeigen"] = new Func<int, ErgebnisAnsicht>(Ansicht),
                ["Berechnen"] = new Func<IReadOnlyList<int>, Action<Laufschritt>, Task<LaufErgebnis>>(Berechnen),
                ["Abbrechen"] = new Action(Abbrechen),
                ["Gaben"] = new Func<WirtschaftlichkeitSeite.Unterdialog,
                                     IReadOnlyDictionary<string, object>>(Unterdialog),
                ["Nachlauf"] = new Func<WirtschaftlichkeitSeite.Unterdialog, bool, string>(Nachlauf),

                ["TitelText"] = T("WIRT_TITEL", "Wirtschaftlichkeit (Kapitalwertmethode DIN EN 17463)")
                                + " — " + T("WIRT_STAMM", "Stamm:") + " " + _stammName,
                ["LabelVarianten"] = T("WIRT_LBL_GRUPPE",
                    "Vergleichsgruppe (Referenz: Stamm, fest gewählt):"),
                ["LabelSzenario"] = T("WIRT_LBL_SZENARIO", "Szenario:"),
                ["LabelFortschritt"] = T("BKS_LBL_FORTSCHRITT", "Fortschritt"),
                ["SpalteArt"] = MyResource.Resource.BK_SP_ART,
                ["SpalteBezeichner"] = MyResource.Resource.BK_SP_BEZEICHNER,
                ["SpalteProjektname"] = MyResource.Resource.BK_SP_PROJEKTNAME,
                ["SpalteSimulation"] = MyResource.Resource.BK_BER_SP_SIMULATION,
                ["PhotovoltaikText"] = T("PVW_KNOPF", "Photovoltaik…"),
                ["BhkwText"] = T("BHW_KNOPF", "BHKW-Wirtschaftlichkeit…"),
                ["StrombezugText"] = T("WIRT_BTN_STROM_TARIF", "Strombezug…"),
                ["ParameterText"] = T("WIRT_BTN_PARAMETER", "Parameter…"),
                ["VerlaufText"] = T("WIRT_BTN_VERLAUF", "Verlauf…"),
                ["BerechnenText"] = T("WIRT_BTN_BERECHNEN", "Berechnen"),
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["MeldungStammReferenz"] = MyResource.Resource.BK_BER_MSG_STAMM_REFERENZ,
                ["StatusAbgebrochen"] = MyResource.Resource.BK_BER_STATUS_ABGEBROCHEN,
                ["HilfeSchluessel"] = "UcWirtschaftlichkeit.btn_Help"
            };
        }

        // =====================================================================
        // Laden (Vorbild LadeDaten + AktualisiereListe + ZeigeParameterzeile)
        // =====================================================================

        private WirtschaftlichkeitStand Laden()
        {
            var stand = new WirtschaftlichkeitStand();

            var zeilen = new List<VarianteZeile>();
            var gewaehlt = new List<int>();
            _namen.Clear();
            try
            {
                foreach (BerichtsDatenSammler.VariantenStatus st in
                         BerichtsDatenSammler.ErmittleStatus(_idStamm, _stammName))
                {
                    zeilen.Add(new VarianteZeile
                    {
                        IdProjekt = st.IdProjekt,
                        Art = st.IstStamm ? MyResource.Resource.BK_ART_STAMM
                                          : MyResource.Resource.BK_ART_VARIANTE,
                        Bezeichner = st.IstStamm ? MyResource.Resource.BK_ART_STAMMPROJEKT
                                                 : st.Variantenname,
                        Projektname = st.Projektname,
                        SimStand = st.SimStandText,
                        IstStamm = st.IstStamm,
                        Auffaellig = !st.SimStand.HasValue || st.Veraltet
                    });
                    // Vorgabe: standardmaessig alle Varianten der Gruppe vergleichen.
                    gewaehlt.Add(st.IdProjekt);
                    _namen[st.IdProjekt] = st.IstStamm
                        ? MyResource.Resource.BK_ART_STAMM
                        : (string.IsNullOrEmpty(st.Variantenname) ? st.Projektname : st.Variantenname);
                }
            }
            catch { }
            stand.Varianten = zeilen;
            stand.GewaehlteVarianten = gewaehlt;

            var szenarien = new List<ValueTuple<int, string>>();
            for (int i = 0; i < SZENARIEN.Length; i++)
                szenarien.Add(new ValueTuple<int, string>(i, SzenarioAnzeige(i)));
            stand.Szenarien = szenarien;
            stand.SzenarioId = 0;

            stand.Parameterzeile = Parameterzeile();

            // Persistierte Ergebnisse anzeigen, solange sie zum Simulationsstand
            // passen (Vorbild LadeDaten).
            try { _ergebnisse = _ctrl.LadeErgebnisse(new List<int>(gewaehlt)); }
            catch { _ergebnisse = new List<WirtschaftlichkeitErgebnis>(); }

            bool veraltet = _ergebnisse.Count > 0 &&
                            _ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x));
            BilanzenAuffrischen();
            stand.Ansicht = Ansicht(0);

            stand.Statuszeile = _ergebnisse.Count == 0
                ? T("WIRT_STATUS_KEINE", "Noch keine Wirtschaftlichkeitsberechnung gespeichert — bitte „Berechnen“.")
                : veraltet
                    ? T("WIRT_STATUS_VERALTET", "⚠ Gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand — bitte „Berechnen“.")
                    : string.Format(T("WIRT_STATUS_STAND", "Gespeicherte Ergebnisse vom {0}."),
                                    _ergebnisse[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm"));

            WirtschaftlichkeitCtrl.ErzeugerFlags flags = null;
            try { flags = _ctrl.ErzeugerDerGruppe(_idStamm); }
            catch { }
            bool tarifAktiv = false;
            try { tarifAktiv = _ctrl.LadeTarif(_idStamm).Aktiv; }
            catch { }

            stand.MitPhotovoltaik = flags != null && flags.Photovoltaik;
            stand.MitBhkw = flags != null && flags.Bhkw;
            stand.MitStrombezug = (flags != null && flags.Waermepumpe) || tarifAktiv;

            return stand;
        }

        /// <summary>
        /// Der AUSWEIS der Bilanzierungsregeln steht NEBEN dem Parameternachweis,
        /// nicht in ihm (L12/L13): eigene Herkunft, eigene Lokalisierung.
        /// </summary>
        private string Parameterzeile()
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                TarifParameter t = _ctrl.LadeTarif(_idStamm);
                return T("WIRT_PARAM_KOPF", "Parameter:") + " " + p.Nachweis(BerichtTexte.Kultur) +
                       " · " + T("WIRT_PARAM_REFERENZ", "Referenz: Stammprojekt · Restwert linear") +
                       " · " + t.Nachweis(BerichtTexte.Kultur) + " · " +
                       BilanzKonvention.Bestimme(p, new GesetzKatalog()).Ausweis(BerichtTexte.Kultur);
            }
            catch { return ""; }
        }

        // =====================================================================
        // Anzeigen (Vorbild ZeigeErgebnisse + KachelnAktualisieren)
        // =====================================================================

        private ErgebnisAnsicht Ansicht(int szenarioId)
        {
            string szenario = SZENARIEN[Math.Max(0, Math.Min(SZENARIEN.Length - 1, szenarioId))];
            CultureInfo kultur = BerichtTexte.Kultur;

            List<WirtschaftlichkeitErgebnis> zeilen = _ergebnisse
                .Where(x => x.Szenario == szenario)
                .OrderByDescending(x => x.IstStamm)
                .ToList();

            var ansicht = new ErgebnisAnsicht { Kacheln = Kacheln(zeilen, kultur) };
            if (zeilen.Count == 0) return ansicht;

            var spalten = new List<string> { T("WIRT_SP_KENNZAHL", "Kennzahl") };
            foreach (WirtschaftlichkeitErgebnis erg in zeilen)
                spalten.Add(_namen.ContainsKey(erg.IdProjekt) ? _namen[erg.IdProjekt]
                            : (erg.IstStamm ? MyResource.Resource.BK_ART_STAMM : erg.Anzeige));

            var matrixzeilen = new List<MatrixZeile>();

            // ETAPPE E7: EINE Zeilendefinition fuer Seite, Word und Excel. Die
            // SICHTBARKEIT entscheidet sich ueber ALLE Ergebnisse der Gruppe,
            // nicht ueber das gerade angezeigte Szenario - sonst zeigten Seite
            // und Bericht verschiedene Tabellen.
            if (_tarifCache == null)
            {
                try { _tarifCache = _ctrl.LadeTarif(_idStamm); }
                catch { _tarifCache = new TarifParameter(); }
            }
            foreach (WirtZeile z in WirtschaftlichkeitZeilen.Kennzahlen(_ergebnisse, _tarifCache))
            {
                bool hatWert = zeilen.Any(x => z.IstText
                    ? !string.IsNullOrEmpty(z.Text(x))
                    : (x.IstStamm && z.StammAnzeige != null) || (z.Wert != null && z.Wert(x).HasValue));
                if (!hatWert) continue;
                matrixzeilen.Add(Zeile(z.Titel, zeilen, x => z.Anzeige(x, kultur)));
            }

            // W3: CO₂-Vermeidung gegenueber getrennter Erzeugung (aus dem Cache;
            // nur fuer Projekte, deren Ergebnis zum Simulationslauf passt).
            if (_bilanzen.Values.Any(x => x != null && x.CO2VermeidungT.HasValue))
                matrixzeilen.Add(Zeile(
                    EmissionsAusweis.BilanzVermeidung(EmissionsAusweis.ModusAusBilanzen(_bilanzen.Values)),
                    zeilen, x =>
                    {
                        EmissionsBilanz b = _bilanzen.ContainsKey(x.IdProjekt) ? _bilanzen[x.IdProjekt] : null;
                        return b == null ? "—" : W(b.CO2VermeidungT, "N1", kultur);
                    }));

            // Hinweiszeilen (nicht-fatal W3 / unvollstaendige Rechnungen).
            string hinweis = T("WIRT_ZEILE_HINWEIS", "Hinweis");
            if (zeilen.Any(x => x.Hinweis != null))
                matrixzeilen.Add(Zeile(hinweis, zeilen, x => x.Hinweis != null ? "⚠ " + x.Hinweis : ""));
            if (zeilen.Any(x => x.Fehlgrund != null))
                matrixzeilen.Add(Zeile(hinweis, zeilen, x => x.Fehlgrund != null ? "⚠ " + x.Fehlgrund : ""));

            KohaerenzZeilen(zeilen, matrixzeilen);

            ansicht.Matrix = new ErgebnisMatrix { Spalten = spalten, Zeilen = matrixzeilen };
            return ansicht;
        }

        private static MatrixZeile Zeile(string titel, List<WirtschaftlichkeitErgebnis> zeilen,
                                         Func<WirtschaftlichkeitErgebnis, string> wert)
        {
            var zellen = new List<string>();
            foreach (WirtschaftlichkeitErgebnis erg in zeilen) zellen.Add(wert(erg));
            return new MatrixZeile { Titel = titel, Zellen = zellen };
        }

        /// <summary>
        /// ETAPPE B2 (Konzept BHKW-Wirtschaftlichkeit § 4.1): die Zeilen der
        /// Kohärenzprüfung, je Hinweis eine Zeile. Sie sind nicht persistiert;
        /// ein aus der Datenbank geladener Stand zeigt sie deshalb nicht.
        /// </summary>
        private void KohaerenzZeilen(List<WirtschaftlichkeitErgebnis> zeilen, List<MatrixZeile> ziel)
        {
            int hoechste = 0;
            foreach (WirtschaftlichkeitErgebnis x in zeilen)
                if (x.KohaerenzHinweise != null && x.KohaerenzHinweise.Count > hoechste)
                    hoechste = x.KohaerenzHinweise.Count;
            if (hoechste == 0) return;

            string titel = T("KOH_ZEILE_TITEL", "Kohärenzprüfung");
            for (int i = 0; i < hoechste; i++)
            {
                int index = i;
                ziel.Add(Zeile(titel, zeilen, x =>
                {
                    List<KohaerenzHinweis> l = x.KohaerenzHinweise;
                    if (l == null || index >= l.Count) return "";
                    KohaerenzHinweis h = l[index];
                    string marke = string.Equals(h.Schwere, KohaerenzSchwere.WARNUNG,
                                                 StringComparison.Ordinal) ? "⚠ " : "· ";
                    return marke + h.Text;
                }));
            }
        }

        /// <summary>
        /// KD6a: die vier Kennzahl-Karten — beste Variante gegenüber Stamm im
        /// gewählten Szenario. Reine ANZEIGE der bereits berechneten Werte.
        /// </summary>
        private List<KachelZeile> Kacheln(List<WirtschaftlichkeitErgebnis> zeilen, CultureInfo kultur)
        {
            var kw = new KachelZeile { Titel = T("WIRT_KACHEL_KW", "Kapitalwert ggue. Stamm") };
            var an = new KachelZeile { Titel = T("WIRT_KACHEL_ANNUITAET", "Annuität") };
            var am = new KachelZeile { Titel = T("WIRT_KACHEL_AMORTISATION", "Amortisation") };
            var irr = new KachelZeile { Titel = T("WIRT_KACHEL_IRR", "Interner Zinsfuß") };

            WirtschaftlichkeitErgebnis beste = null;
            foreach (WirtschaftlichkeitErgebnis x in zeilen)
                if (!x.IstStamm && x.KapitalwertDiff.HasValue &&
                    (beste == null || x.KapitalwertDiff.Value > beste.KapitalwertDiff.Value))
                    beste = x;

            if (beste != null)
            {
                string name = _namen.ContainsKey(beste.IdProjekt) ? _namen[beste.IdProjekt] : beste.Anzeige;
                string quelle = string.Format(T("WIRT_KACHEL_BESTE", "beste Variante: {0}"), name);

                kw.Wert = beste.KapitalwertDiff.Value.ToString("N0", kultur) + " €";
                kw.Quelle = quelle;
                an.Wert = beste.AnnuitaetKW.HasValue
                    ? beste.AnnuitaetKW.Value.ToString("N0", kultur) + " €/a" : "—";
                an.Quelle = quelle;
                am.Wert = beste.AmortisationJahre.HasValue
                    ? beste.AmortisationJahre.Value.ToString("N1", kultur) + " a"
                    : T("WIRT_KACHEL_KEINE", "keine");
                am.Quelle = quelle;
                irr.Wert = beste.IRR.HasValue ? beste.IRR.Value.ToString("N1", kultur) + " %" : "—";
                irr.Quelle = quelle;
            }
            else
            {
                WirtschaftlichkeitErgebnis stamm = zeilen.Find(x => x.IstStamm);
                string q = T("WIRT_KACHEL_NUR_STAMM", "nur Stammprojekt gerechnet");
                kw.Wert = stamm != null && stamm.Kapitalwert.HasValue
                    ? stamm.Kapitalwert.Value.ToString("N0", kultur) + " €" : "—";
                kw.Quelle = stamm != null
                    ? T("WIRT_KACHEL_STAMM_KW", "Nettobarwert des Stammprojekts") : "";
                an.Wert = "—"; an.Quelle = q;
                am.Wert = "—"; am.Quelle = q;
                irr.Wert = "—"; irr.Quelle = q;
            }

            return new List<KachelZeile> { kw, an, am, irr };
        }

        /// <summary>Emissionsbilanz-Cache neu füllen (nur aktuelle Ergebnisse, W3).</summary>
        private void BilanzenAuffrischen()
        {
            _bilanzen.Clear();
            try
            {
                _parameterCache = _ctrl.LadeParameter(_idStamm);
                if (_parameterCache.IdKraftwerkspark <= 0) return;
                foreach (WirtschaftlichkeitErgebnis erg in _ergebnisse
                         .Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET))
                {
                    if (_bilanzen.ContainsKey(erg.IdProjekt)) continue;
                    _bilanzen[erg.IdProjekt] = _ctrl.ErgebnisAktuell(erg)
                        ? EmissionsBilanzRechner.Berechne(erg.IdProjekt, _parameterCache)
                        : null;
                }
            }
            catch { }
        }

        // =====================================================================
        // Berechnen (Vorbild btnBerechnen_Click)
        // =====================================================================

        private async Task<LaufErgebnis> Berechnen(IReadOnlyList<int> variantenIds,
                                                   Action<Laufschritt> melder)
        {
            if (_cts != null) return new LaufErgebnis { Abgebrochen = true };

            var varianten = new List<int>(variantenIds ?? new List<int>());
            _cts = new CancellationTokenSource();
            var melde = new Progress<BerichtsDatenSammler.Fortschritt>(
                f => melder(new Laufschritt(f.Aktuell, f.Gesamt, f.Text)));

            try
            {
                CancellationToken ct = _cts.Token;
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                TarifParameter tarif = _ctrl.LadeTarif(_idStamm);

                // W3: Tarifmatrix und KWKG-Split brauchen Stundenreihen — dann
                // wird je Projekt frisch in-memory simuliert.
                bool mitZeitreihen = tarif.Aktiv || p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;

                _ergebnisse = await Task.Run(() =>
                {
                    BerichtsDaten daten = new BerichtsDatenSammler().Sammle(
                        _idStamm, _stammName, varianten, false, mitZeitreihen, melde, ct);
                    return _ctrl.Berechne(daten, p);
                }, ct);

                _tarifCache = null;
                BilanzenAuffrischen();

                return new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = string.Format(
                        T("WIRT_STATUS_BERECHNET",
                          "Berechnet am {0} — Ergebnisse gespeichert (Basis für den Berichts-Baustein Wirtschaftlichkeit)."),
                        DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
                };
            }
            catch (OperationCanceledException)
            {
                return new LaufErgebnis { Abgebrochen = true };
            }
            catch (Exception ex)
            {
                return new LaufErgebnis
                {
                    Fehler = string.Format(
                        T("WIRT_MSG_RECHENFEHLER", "Fehler bei der Wirtschaftlichkeitsberechnung: {0}"),
                        ex.Message)
                };
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        private void Abbrechen()
        {
            if (_cts != null) _cts.Cancel();
        }

        // =====================================================================
        // Die fünf Unterdialoge
        // =====================================================================

        private Func<bool> _verlaufNeuGesammelt;

        private IReadOnlyDictionary<string, object> Unterdialog(
            WirtschaftlichkeitSeite.Unterdialog art)
        {
            try
            {
                switch (art)
                {
                    case WirtschaftlichkeitSeite.Unterdialog.Photovoltaik:
                        return PhotovoltaikVerguetungHuelle.Gaben(_idStamm, _besitzer);

                    case WirtschaftlichkeitSeite.Unterdialog.Bhkw:
                        string titel;
                        return BhkwWirtschaftlichkeitHuelle.Gaben(_idStamm, _ergebnisse, out titel);

                    case WirtschaftlichkeitSeite.Unterdialog.Strombezug:
                        return TarifstrukturHuelle.Gaben(_idStamm, TarifSicht.Strombezug);

                    case WirtschaftlichkeitSeite.Unterdialog.Parameter:
                        return WirtschaftlichkeitParameterHuelle.Gaben(_idStamm);

                    case WirtschaftlichkeitSeite.Unterdialog.Verlauf:
                        var varianten = new List<int>();
                        foreach (WirtschaftlichkeitErgebnis e in _ergebnisse)
                            if (!e.IstStamm && !varianten.Contains(e.IdProjekt)) varianten.Add(e.IdProjekt);
                        return KapitalwertVerlaufHuelle.Gaben(
                            _idStamm, _stammName, varianten, out _verlaufNeuGesammelt);
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Was nach einem Unterdialog zu melden ist — wortgleich die Sätze, die
        /// der Vorläufer in seine Statuszeile schrieb.
        /// </summary>
        private string Nachlauf(WirtschaftlichkeitSeite.Unterdialog art, bool gespeichert)
        {
            _tarifCache = null;   // E7: Beschriftung der Stromkostenzeile neu holen

            if (art == WirtschaftlichkeitSeite.Unterdialog.Verlauf)
            {
                // Der Verlaufsdialog kann neu simuliert haben (Review Phase 11):
                // Dann passen die persistierten Ergebnisse nicht mehr zum
                // Simulationsstand — und das gehoert gesagt.
                bool neu = _verlaufNeuGesammelt != null && _verlaufNeuGesammelt();
                _verlaufNeuGesammelt = null;
                if (!neu) return "";

                return _ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x))
                    ? T("WIRT_MELD_VERLAUF_NEU",
                        "⚠ Für den Verlauf wurde neu simuliert — gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand, bitte „Berechnen“.")
                    : "";
            }

            if (!gespeichert) return "";

            switch (art)
            {
                case WirtschaftlichkeitSeite.Unterdialog.Photovoltaik:
                    return T("PVW_MELD_GESPEICHERT", "PV-Vergütung gespeichert — bitte neu berechnen.");
                case WirtschaftlichkeitSeite.Unterdialog.Bhkw:
                    return T("BHW_MELD_GESPEICHERT",
                             "BHKW-Wirtschaftlichkeit gespeichert — bitte neu berechnen.");
                case WirtschaftlichkeitSeite.Unterdialog.Strombezug:
                    return T("WIRT_MELD_TARIF", "Tarifstruktur gespeichert — bitte neu berechnen.");
                case WirtschaftlichkeitSeite.Unterdialog.Parameter:
                    return T("WIRT_MELD_PARAMETER", "Parameter gespeichert — bitte neu berechnen.");
            }
            return "";
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>
        /// Der Anzeigetext eines Szenarios. Bis eine Übersetzung vorliegt, ist
        /// es der Persistenzwert selbst (unverändert zum Vorläufer, W1-O6).
        /// </summary>
        private static string SzenarioAnzeige(int nummer)
        {
            string schluessel = nummer == 1 ? "WIRT_SZEN_BEST"
                              : nummer == 2 ? "WIRT_SZEN_WORST" : "WIRT_SZEN_ERWARTET";
            return T(schluessel, SZENARIEN[nummer]);
        }

        private static string W(double? v, string format, CultureInfo kultur)
        {
            return v.HasValue ? v.Value.ToString(format, kultur) : "—";
        }

        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string t = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(t) ? rueckfall : t;
            }
            catch { return rueckfall; }
        }
    }
}
