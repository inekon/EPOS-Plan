using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Kostenseite (iU9-W5.4/W5.6) — Nachfolge von
    /// <c>Views/BerichteKosten/UcBkKosten.cs</c> (1 311 Z.).
    ///
    /// <para><b>Keine eigene Rechenwelt.</b> Investition und Betrieb kommen aus
    /// derselben Leselogik wie die Kapitalwertrechnung
    /// (<c>WirtschaftlichkeitCtrl.LiesInvestitionen</c> /
    /// <c>LiesBetriebskosten</c>), die Energiekosten sind der zuletzt
    /// GESPEICHERTE Wert der Wirtschaftlichkeitsrechnung, die Anlagensummen
    /// kommen aus <see cref="KostenSummenCtrl"/> und die Energieträger über
    /// dieselbe Vorrangkette wie im <c>KostenEmissionRechner</c> (Projektwert
    /// vor Katalogwert).</para>
    ///
    /// <para><b>„—" statt 0,00</b> (Nutzerentscheidung 4 vom 18.08.2026): Keine
    /// Position heißt nicht „kostet nichts". Alle Werte werden hier fertig
    /// formatiert; die Komponente rechnet nicht.</para>
    /// </summary>
    internal sealed class KostenSeiteGaben
    {
        private readonly Func<Form> _besitzer;

        private int _idProjekt = -1;
        private string _projektname = "";

        private readonly WirtschaftlichkeitCtrl _wirt = new WirtschaftlichkeitCtrl();

        /// <summary>Die Vergleichsgruppe (W5‑B‑5): der Stamm, von der Rahmenhülle gesetzt.</summary>
        private int _idStamm = -1;
        private string _stammName = "";

        /// <summary>Die Ids der Gruppe in Listenreihenfolge (Stamm zuerst) — für die Vergleichswahl.</summary>
        private readonly List<int> _gruppe = new List<int>();

        /// <summary>Die geteilte Vergleichswahl der drei Seiten (W5‑B‑5); die Rahmenhülle setzt sie.</summary>
        internal Vergleichsauswahl Vergleich { get; set; } = new Vergleichsauswahl();

        // Befundlisten der Fußzeile — wortgleich zum Vorläufer.
        private readonly List<string> _ohnePosition = new List<string>();
        private readonly List<string> _nichtVerbaut = new List<string>();
        private readonly List<string> _traegerOhnePreis = new List<string>();
        private readonly List<string> _traegerNichtZugeordnet = new List<string>();
        private readonly List<string> _traegerOhneHeizwert = new List<string>();

        /// <summary>Die zuletzt gebauten Zeilen (Schlüssel → Anlage bzw. Komponente).</summary>
        private readonly Dictionary<int, ProjektEnergietraegerCtrl.AnlagenEintrag> _anlagen =
            new Dictionary<int, ProjektEnergietraegerCtrl.AnlagenEintrag>();
        private readonly Dictionary<int, string> _loseKomponenten = new Dictionary<int, string>();

        internal KostenSeiteGaben(Func<Form> besitzer)
        {
            _besitzer = besitzer;
        }

        /// <summary>Setzt das anzuzeigende Projekt (Stamm ODER Variante).</summary>
        internal void SetzeProjekt(int idProjekt, string projektname)
        {
            _idProjekt = idProjekt;
            _projektname = projektname ?? "";
        }

        /// <summary>Setzt die Vergleichsgruppe (den Stamm) für die Kostengegenüberstellung (W5‑B‑5).</summary>
        internal void SetzeGruppe(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
        }

        /// <summary>Der Parametersatz der Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<KostenStand>(Laden),
                ["VerwaltungGaben"] = new Func<KostenZeile, IReadOnlyDictionary<string, object>>(
                    VerwaltungGaben),
                ["TraegerGaben"] = new Func<IReadOnlyDictionary<string, object>>(TraegerGaben),
                ["LoeschFrage"] = new Func<KostenZeile, string>(LoeschFrage),
                ["Loeschen"] = new Func<KostenZeile, string>(Loeschen),
                ["VergleichGewaehlt"] = new Action<IReadOnlyList<int>>(VergleichSetzen),
                ["LabelVergleich"] = MyResource.Resource.BK_LBL_VERGLEICHSWAHL,
                ["StammFestTipp"] = MyResource.Resource.BK_BER_MSG_STAMM_REFERENZ,

                ["LabelKomponenten"] = MyResource.Resource.BK_KOSTEN_LBL_KOMPONENTEN,
                ["LabelTraeger"] = MyResource.Resource.BK_KOSTEN_LBL_TRAEGER,
                ["SpalteAktionen"] = T("BK_KOSTEN_SP_AKTIONEN", "Aktionen"),
                ["SpalteAnlage"] = T("BK_KOSTEN_SP_ANLAGE", "Anlage / Komponente"),
                ["SpalteSumme"] = MyResource.Resource.BK_KOSTEN_SP_SUMME,
                ["SpalteBetrieb"] = T("BK_KOSTEN_SP_BETRIEB", "Betrieb [€/a]"),
                ["VerwaltungText"] = MyResource.Resource.BK_KOSTEN_BTN_VERWALTUNG,
                // Ä25 (10.09.2026): Der Bereichskopf trägt den Namen der Maske —
                // derselbe Schlüssel, mit dem sie als eigenes Fenster überschrieben ist
                // (KostenKomponenteHuelle), ohne den Gewerke-Platzhalter.
                ["VerwaltungTitel"] = T("KDLG_TITEL", "Kostenverwaltung {0}").Replace(" {0}", ""),
                ["TraegerText"] = T("BK_KOSTEN_BTN_TRAEGER", "Energieträgerverwaltung…"),
                ["WahlKurztext"] = T("BKS_WAHL_ANLAGE", "Anlage wählen"),
                ["LoeschenKurztext"] = T("BK_KOSTEN_LOSE_TITEL", "Positionen ohne Anlagenzuordnung"),
                ["LoeschenTitel"] = T("BK_KOSTEN_LOSE_TITEL", "Positionen ohne Anlagenzuordnung"),
                ["JaText"] = T("BKS_BTN_JA", "Ja"),
                ["NeinText"] = T("BKS_BTN_NEIN", "Nein"),
                ["HilfeSchluessel"] = "UcBkKosten.btn_Help"
            };
        }

        // =====================================================================
        // Laden (Vorbild UcBkKosten.Aktualisiere)
        // =====================================================================

        private KostenStand Laden()
        {
            var stand = new KostenStand();
            _anlagen.Clear();
            _loseKomponenten.Clear();

            if (_idProjekt <= 0)
            {
                stand.Projektzeile = MyResource.Resource.BK_KOSTEN_KEIN_PROJEKT;
                stand.Bedienbar = false;
                stand.Kacheln = LeereKacheln();
                stand.TraegerSpalten = Traegerspalten();
                return stand;
            }

            stand.Bedienbar = true;
            stand.Projektzeile = string.Format(MyResource.Resource.BK_KOSTEN_PROJEKT, _projektname);

            // ET-2 (Anwenderbefund 08.09.2026): Die elektrische Welt bekommt ihren
            // Stromtraeger, bevor die Traegertabelle gelesen wird - die rote Fehlzeile
            // "Elektrische Energie - nicht zugeordnet" (BK1) heilt sich damit selbst.
            try { ProjektEnergietraegerCtrl.StromTraegerSicherstellen(_idProjekt); }
            catch { }

            CultureInfo kultur = BerichtTexte.Kultur;

            // Die drei Kategorien des Projekts - dieselbe Leselogik wie die
            // Gegenueberstellung der Gruppe (W5-B-5), EINMAL geschrieben (Kostenwerte).
            Kostenwerte w = Kostenwerte.Lies(_wirt, _idProjekt);
            int investPositionen = w.InvestPositionen;
            string energieHinweis = w.EnergieHinweis;
            bool energieNull = w.EnergieNull;

            var kInvest = new KachelZeile
            {
                Titel = MyResource.Resource.BK_KOSTEN_INVEST,
                Wert = w.InvestText(kultur),
                Quelle = w.Zuschuss > 0
                    ? string.Format(MyResource.Resource.BK_KOSTEN_ZUSCHUSS, w.Zuschuss.ToString("N2", kultur))
                    : MyResource.Resource.BK_KOSTEN_INVEST_HINT
            };
            var kBetrieb = new KachelZeile
            {
                Titel = MyResource.Resource.BK_KOSTEN_BETRIEB,
                Wert = w.BetriebText(kultur),
                Quelle = MyResource.Resource.BK_KOSTEN_BETRIEB_HINT
            };
            var kEnergie = new KachelZeile
            {
                Titel = MyResource.Resource.BK_KOSTEN_ENERGIE,
                Wert = w.EnergieText(kultur),
                Quelle = MyResource.Resource.BK_KOSTEN_ENERGIE_HINT
            };

            stand.Kacheln = new List<KachelZeile> { kInvest, kBetrieb, kEnergie };
            Gegenueberstellung(stand, kultur, w);
            stand.Komponenten = Komponenten(kultur);
            stand.TraegerSpalten = Traegerspalten();
            stand.Traeger = Traeger(kultur);
            stand.Statuszeile = Statuszeile(investPositionen, energieHinweis, energieNull);
            return stand;
        }

        /// <summary>
        /// KOSTEN IM VERGLEICH (Anwenderwunsch 08.09.2026, W5‑B‑5): die drei Kategorien je
        /// Version der Gruppe nebeneinander, Stamm zuerst — nur die Versionen der geteilten
        /// Vergleichswahl. Ohne Gruppe (kein Stamm bekannt) bleibt der Block leer. Die Werte
        /// des angezeigten Projekts kommen aus derselben Lesung wie seine Karten.
        /// </summary>
        private void Gegenueberstellung(KostenStand stand, CultureInfo kultur, Kostenwerte werteProjekt)
        {
            _gruppe.Clear();
            if (_idStamm <= 0) return;

            var versionen = new List<VarianteZeile>();
            try
            {
                foreach (VariantenCtrl.VarianteInfo vi in new VariantenCtrl().LadeGruppe(_idStamm, _stammName))
                {
                    versionen.Add(new VarianteZeile
                    {
                        IdProjekt = vi.IdProjekt,
                        Art = vi.IstStamm ? MyResource.Resource.BK_ART_STAMM
                                          : MyResource.Resource.BK_ART_VARIANTE,
                        Bezeichner = vi.IstStamm ? MyResource.Resource.BK_ART_STAMMPROJEKT
                                                 : vi.Variantenname,
                        Projektname = vi.Projektname,
                        IstStamm = vi.IstStamm
                    });
                    _gruppe.Add(vi.IdProjekt);
                }
            }
            catch { }
            stand.Versionen = versionen;
            List<int> gewaehlt = Vergleich.Gewaehlte(_gruppe, _idStamm);
            stand.GewaehlteVarianten = gewaehlt;
            if (versionen.Count == 0) return;

            stand.VergleichTitel = MyResource.Resource.BK_KOSTEN_LBL_VERGLEICH;
            var spalten = new List<string> { T("WIRT_SP_KENNZAHL", "Kennzahl") };
            var invest = new List<string>();
            var betrieb = new List<string>();
            var energie = new List<string>();
            foreach (VarianteZeile v in versionen)
            {
                if (!gewaehlt.Contains(v.IdProjekt)) continue;
                spalten.Add(v.IstStamm ? v.Art
                            : (string.IsNullOrEmpty(v.Bezeichner) ? v.Projektname : v.Bezeichner));
                Kostenwerte w = v.IdProjekt == _idProjekt ? werteProjekt : Kostenwerte.Lies(_wirt, v.IdProjekt);
                invest.Add(w.InvestText(kultur));
                betrieb.Add(w.BetriebText(kultur));
                energie.Add(w.EnergieText(kultur));
            }
            stand.VergleichSpalten = spalten;
            stand.Vergleich = new List<MatrixZeile>
            {
                new MatrixZeile
                {
                    Titel = MyResource.Resource.BK_KOSTEN_INVEST + " [" + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR + "]",
                    Zellen = invest
                },
                new MatrixZeile
                {
                    Titel = MyResource.Resource.BK_KOSTEN_BETRIEB + " [" + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR_A + "]",
                    Zellen = betrieb
                },
                new MatrixZeile
                {
                    Titel = MyResource.Resource.BK_KOSTEN_ENERGIE + " [" + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR_A + "]",
                    Zellen = energie
                }
            };
        }

        /// <summary>Die Vergleichswahl der Seite (W5‑B‑5) in die geteilte Auswahl.</summary>
        private void VergleichSetzen(IReadOnlyList<int> gewaehlt)
        {
            Vergleich.Setzen(gewaehlt, _gruppe, _idStamm);
        }

        /// <summary>
        /// Die drei Kategorien EINES Projekts, gelesen wie die Karten des Vorläufers
        /// (<c>UcBkKosten.Aktualisiere</c>): Investition und Betrieb über die Leselogik der
        /// Kapitalwertrechnung (Kategorie 1 und 2, Szenario „Erwartet"), Energie als zuletzt
        /// GESPEICHERTER Wert der Wirtschaftlichkeit. Seit W5‑B‑5 auch je Version der Gruppe.
        /// </summary>
        private sealed class Kostenwerte
        {
            internal double Invest, Zuschuss, Betrieb;
            internal int InvestPositionen, BetriebPositionen;
            internal double? Energie;
            internal string EnergieHinweis = "";
            internal bool EnergieNull = true;

            internal static Kostenwerte Lies(WirtschaftlichkeitCtrl wirt, int idProjekt)
            {
                var w = new Kostenwerte();

                // --- Kategorie 1: Investition (Leselogik der Kapitalwertrechnung) ---
                try
                {
                    // ETAPPE K5: dieselbe Leseueberladung wie der Rechenkern — die
                    // Zuschusszeilen kommen getrennt heraus.
                    double zuschuss;
                    var positionen = WirtschaftlichkeitCtrl.LiesInvestitionen(
                        idProjekt, WirtschaftlichkeitSzenario.ERWARTET, out zuschuss);
                    w.Zuschuss = zuschuss;
                    w.InvestPositionen = positionen.Count;
                    foreach (KapitalwertRechner.InvestPosition p in positionen) w.Invest += p.Betrag;
                }
                catch { }

                // --- Kategorie 2: Betrieb ---
                try
                {
                    w.Betrieb = WirtschaftlichkeitCtrl.LiesBetriebskosten(
                        idProjekt, WirtschaftlichkeitSzenario.ERWARTET);
                    DataTable bt = KostenSummenCtrl.LiesKomponentenSummen(
                        idProjekt, KostenSummenCtrl.KATEGORIE_BETRIEB);
                    w.BetriebPositionen = bt != null ? bt.Rows.Count : 0;
                }
                catch { }

                // --- Energie: zuletzt GESPEICHERTER Wert der Wirtschaftlichkeit ---
                try
                {
                    WirtschaftlichkeitErgebnis erg = wirt
                        .LadeErgebnisse(new List<int> { idProjekt })
                        .FirstOrDefault(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                    if (erg == null || !erg.EnergiekostenJahr.HasValue)
                        w.EnergieHinweis = MyResource.Resource.BK_KOSTEN_ENERGIE_FEHLT;
                    else
                    {
                        w.Energie = erg.EnergiekostenJahr.Value;
                        w.EnergieNull = Math.Abs(erg.EnergiekostenJahr.Value) < 0.005;
                        w.EnergieHinweis = string.Format(MyResource.Resource.BK_KOSTEN_STAND,
                            erg.Zeitstempel.ToString("dd.MM.yyyy HH:mm"));
                    }
                }
                catch { }
                return w;
            }

            internal string InvestText(CultureInfo kultur)
            {
                return InvestPositionen > 0
                    ? Invest.ToString("N2", kultur) + " " + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR
                    : "—";
            }

            internal string BetriebText(CultureInfo kultur)
            {
                return BetriebPositionen > 0
                    ? Betrieb.ToString("N2", kultur) + " " + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR_A
                    : "—";
            }

            internal string EnergieText(CultureInfo kultur)
            {
                return Energie.HasValue
                    ? Energie.Value.ToString("N2", kultur) + " " + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR_A
                    : "—";
            }
        }

        private List<KachelZeile> LeereKacheln()
        {
            return new List<KachelZeile>
            {
                new KachelZeile { Titel = MyResource.Resource.BK_KOSTEN_INVEST, Wert = "—",
                                  Quelle = MyResource.Resource.BK_KOSTEN_INVEST_HINT },
                new KachelZeile { Titel = MyResource.Resource.BK_KOSTEN_BETRIEB, Wert = "—",
                                  Quelle = MyResource.Resource.BK_KOSTEN_BETRIEB_HINT },
                new KachelZeile { Titel = MyResource.Resource.BK_KOSTEN_ENERGIE, Wert = "—",
                                  Quelle = MyResource.Resource.BK_KOSTEN_ENERGIE_HINT }
            };
        }

        private string Statuszeile(int investPositionen, string energieHinweis, bool energieNull)
        {
            string status = string.Format(MyResource.Resource.BK_KOSTEN_STATUS,
                                          investPositionen, energieHinweis).Trim();

            if (_ohnePosition.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_OHNE_POSITION,
                                                  string.Join(", ", _ohnePosition.ToArray()));
            if (_nichtVerbaut.Count > 0)
                status += "  ·  " + string.Format(
                    T("BK_KOSTEN_STATUS_NICHT_VERBAUT", "Kostenpositionen ohne verbaute Anlage: {0}"),
                    string.Join(", ", _nichtVerbaut.ToArray()));
            if (energieNull && _traegerOhnePreis.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_ENERGIE_PREIS0,
                                                  string.Join(", ", _traegerOhnePreis.ToArray()));
            if (_traegerNichtZugeordnet.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_TRAEGER_FEHLT,
                                                  string.Join(", ", _traegerNichtZugeordnet.ToArray()));
            if (_traegerOhneHeizwert.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_TRAEGER_HI0,
                                                  string.Join(", ", _traegerOhneHeizwert.ToArray()));
            return status;
        }

        // =====================================================================
        // Anlagen und Komponenten (Vorbild LadeKomponenten)
        // =====================================================================

        private List<KostenZeile> Komponenten(CultureInfo kultur)
        {
            _ohnePosition.Clear();
            _nichtVerbaut.Clear();
            var zeilen = new List<KostenZeile>();
            int schluessel = 1;

            // Ä21: Selbstheilung VOR dem Lesen — verwaiste Zuordnungen kommen
            // ueber den Geraeteanker zurueck an ihre Anlage.
            try { KostenProjektPositionenCtrl.ZuordnungReparieren(_idProjekt); } catch { }

            try
            {
                List<ProjektEnergietraegerCtrl.AnlagenEintrag> anlagen =
                    ProjektEnergietraegerCtrl.AnlagenMitTraeger(_idProjekt);
                var anlagenIds = new HashSet<int>();
                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen) anlagenIds.Add(a.AnlageId);

                var investAnlage = new Dictionary<int, double>();
                var betriebAnlage = new Dictionary<int, double>();
                var investLose = new Dictionary<string, double>(StringComparer.Ordinal);
                var betriebLose = new Dictionary<string, double>(StringComparer.Ordinal);
                var mitPositionen = new HashSet<string>(StringComparer.Ordinal);
                AnlagenSummenLesen(KostenSummenCtrl.KATEGORIE_INVESTITION, anlagenIds,
                                   investAnlage, investLose, mitPositionen);
                AnlagenSummenLesen(KostenSummenCtrl.KATEGORIE_BETRIEB, anlagenIds,
                                   betriebAnlage, betriebLose, mitPositionen);

                double summe = 0, summeBetrieb = 0;
                var rotGemeldet = new HashSet<string>(StringComparer.Ordinal);

                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen)
                {
                    double invest, bWert;
                    bool hatI = investAnlage.TryGetValue(a.AnlageId, out invest);
                    bool hatB = betriebAnlage.TryGetValue(a.AnlageId, out bWert);
                    if (hatI) summe += invest;
                    if (hatB) summeBetrieb += bWert;

                    var z = new KostenZeile
                    {
                        Schluessel = schluessel++,
                        Anzeige = string.IsNullOrEmpty(a.Bezeichner)
                            ? a.Komponente : a.Komponente + " — " + a.Bezeichner,
                        Summe = hatI ? invest.ToString("N2", kultur) : "—",
                        Betrieb = hatB ? bWert.ToString("N2", kultur) : "—",
                        TraegerId = a.CarrierId
                    };
                    _anlagen[z.Schluessel] = a;

                    if (!mitPositionen.Contains(a.Komponente))
                    {
                        // Die KOMPONENTE hat nirgends eine Position: FEHLENDE
                        // EINGABE, kein Nullbetrag (Nutzerentscheidung 4).
                        z.Art = ZeilenArt.OhnePosition;
                        z.Kurztext = string.Format(
                            MyResource.Resource.BK_KOSTEN_OHNE_POSITION_HINT, a.Komponente);
                        if (!rotGemeldet.Contains(a.Komponente))
                        {
                            rotGemeldet.Add(a.Komponente);
                            _ohnePosition.Add(a.Komponente);
                        }
                    }
                    else if (!hatI && !hatB)
                    {
                        z.Kurztext = T("BK_KOSTEN_ANLAGE_OHNE_POSITIONEN",
                            "Diese Anlage führt keine eigenen Positionen — „Kosten bearbeiten…“ im Anlagendialog oder die Kostenverwaltung pflegt sie je Anlage.");
                    }
                    zeilen.Add(z);
                }

                // Positionen ohne (gueltigen) Anlagenbezug, in zwei Klassen
                // (Ä24): nicht anlagenfaehige Erfassungsgruppen erscheinen als
                // gewoehnliche Zeile, anlagenfaehige gelb.
                var reste = new List<string>();
                foreach (string k in investLose.Keys) reste.Add(k);
                foreach (string k in betriebLose.Keys) if (!reste.Contains(k)) reste.Add(k);

                var resteGelb = new List<string>();
                foreach (string k in reste)
                {
                    double invest, bWert;
                    bool hatI = investLose.TryGetValue(k, out invest);
                    bool hatB = betriebLose.TryGetValue(k, out bWert);
                    if (KostenVorlagenCtrl.IstWaehlbar(k)) { resteGelb.Add(k); continue; }
                    if (hatI) summe += invest;
                    if (hatB) summeBetrieb += bWert;

                    var z = new KostenZeile
                    {
                        Schluessel = schluessel++,
                        Anzeige = k,
                        Summe = hatI ? invest.ToString("N2", kultur) : "—",
                        Betrieb = hatB ? bWert.ToString("N2", kultur) : "—"
                    };
                    var gruppe = new ProjektEnergietraegerCtrl.AnlagenEintrag();
                    gruppe.Komponente = k;   // AnlageId 0: Verwaltung oeffnet die Komponente
                    _anlagen[z.Schluessel] = gruppe;
                    zeilen.Add(z);
                }

                foreach (string k in resteGelb)
                {
                    double invest, bWert;
                    bool hatI = investLose.TryGetValue(k, out invest);
                    bool hatB = betriebLose.TryGetValue(k, out bWert);
                    if (hatI) summe += invest;
                    if (hatB) summeBetrieb += bWert;

                    var z = new KostenZeile
                    {
                        Schluessel = schluessel++,
                        Anzeige = string.Format(
                            T("BK_KOSTEN_NICHT_VERBAUT", "{0} — ohne Anlagenzuordnung"), k),
                        Summe = hatI ? invest.ToString("N2", kultur) : "—",
                        Betrieb = hatB ? bWert.ToString("N2", kultur) : "—",
                        Art = ZeilenArt.OhneZuordnung,
                        Loeschbar = true,
                        Kurztext = T("BK_KOSTEN_NICHT_VERBAUT_HINT",
                            "Kostenpositionen ohne (gültige) Anlagenzuordnung — sie rechnen in der Wirtschaftlichkeit mit. Der Papierkorb löscht sie nach Rückfrage; bearbeiten: Kostenverwaltung, Eintrag „(ohne Anlagenzuordnung)“.")
                    };
                    _loseKomponenten[z.Schluessel] = k;
                    var gruppe = new ProjektEnergietraegerCtrl.AnlagenEintrag();
                    gruppe.Komponente = k;
                    _anlagen[z.Schluessel] = gruppe;
                    zeilen.Add(z);
                    _nichtVerbaut.Add(k);
                }

                if (zeilen.Count > 0)
                    zeilen.Add(new KostenZeile
                    {
                        Anzeige = MyResource.Resource.BK_KOSTEN_SUMME,
                        Summe = summe.ToString("N2", kultur),
                        Betrieb = summeBetrieb.ToString("N2", kultur),
                        Art = ZeilenArt.Summe
                    });
            }
            catch { }

            return zeilen;
        }

        /// <summary>
        /// Ä20: Summen einer Kategorie je Anlage; „lose" Zeilen (NULL oder
        /// gelöschte Anlage) laufen je Komponente auf. Rückfall ohne Spalte:
        /// Komponentensummen als lose Zeilen, damit nichts verschwindet.
        /// </summary>
        private void AnlagenSummenLesen(int kategorie, HashSet<int> anlagenIds,
            Dictionary<int, double> jeAnlage, Dictionary<string, double> jeLose,
            HashSet<string> mitPositionen)
        {
            try
            {
                DataTable t = KostenSummenCtrl.LiesAnlagenSummen(_idProjekt, kategorie);
                if (t == null)
                {
                    DataTable alt = KostenSummenCtrl.LiesKomponentenSummen(_idProjekt, kategorie);
                    if (alt != null)
                        foreach (DataRow r in alt.Rows)
                        {
                            double? w = D(r, "Summe");
                            if (!w.HasValue) continue;
                            string k = S(r, "Komponente");
                            jeLose[k] = w.Value;
                            mitPositionen.Add(k);
                        }
                    return;
                }
                foreach (DataRow r in t.Rows)
                {
                    double? w = D(r, "Summe");
                    if (!w.HasValue) continue;
                    string k = S(r, "Komponente");
                    mitPositionen.Add(k);
                    bool lose = r["ID_Anlage"] == DBNull.Value ||
                                !anlagenIds.Contains(Convert.ToInt32(r["ID_Anlage"]));
                    if (lose)
                    {
                        double alt2;
                        jeLose.TryGetValue(k, out alt2);
                        jeLose[k] = alt2 + w.Value;
                    }
                    else jeAnlage[Convert.ToInt32(r["ID_Anlage"])] = w.Value;
                }
            }
            catch { }
        }

        // =====================================================================
        // Energieträger (Vorbild LadeTraeger)
        // =====================================================================

        private List<string> Traegerspalten()
        {
            return new List<string>
            {
                MyResource.Resource.BK_KOSTEN_SP_TRAEGER,
                MyResource.Resource.BK_KOSTEN_SP_ABRECHNUNG,
                MyResource.Resource.BK_KOSTEN_SP_HEIZWERT,
                MyResource.Resource.BK_KOSTEN_SP_ARBEITSPREIS,
                MyResource.Resource.BK_KOSTEN_SP_ARBEITSPREIS_KWH,
                MyResource.Resource.BK_KOSTEN_SP_GRUNDPREIS,
                T("BK_KOSTEN_SP_LEISTUNGSPREIS", "Leistungspreis [€/(kW·a)]"),
                T("BK_KOSTEN_SP_CO2", "CO₂ [g/kWh]"),
                T("BK_KOSTEN_SP_SO2", "SO₂ [mg/kWh]"),
                T("BK_KOSTEN_SP_NOX", "NOx [mg/kWh]")
            };
        }

        private List<TraegerZeile> Traeger(CultureInfo kultur)
        {
            _traegerOhnePreis.Clear();
            _traegerNichtZugeordnet.Clear();
            _traegerOhneHeizwert.Clear();

            var zeilen = new List<TraegerZeile>();

            // Die VERWENDUNGSMENGE — die eine Frage, die die gespeicherte
            // Abfrage nicht beantworten kann.
            var verwendet = new Dictionary<int, ProjektEnergietraegerCtrl.Verwendung>();
            try
            {
                foreach (ProjektEnergietraegerCtrl.Verwendung v in
                         ProjektEnergietraegerCtrl.Verwendete(_idProjekt))
                {
                    verwendet[v.CarrierId] = v;
                    if (!v.Zugeordnet)
                        _traegerNichtZugeordnet.Add(v.Name.Length > 0
                            ? v.Name : "#" + v.CarrierId.ToString(kultur));
                }
            }
            catch { verwendet.Clear(); }

            // Der Berechnungsmodus des Projekts (F7) gilt fuer die CO₂-Spalte.
            string emissionsModus;
            try { emissionsModus = EmissionenCtrl.ModusFuerRechenlauf(_idProjekt); }
            catch { emissionsModus = DbWerte.EMISSION_MODUS_CO2; }

            var angezeigt = new HashSet<int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT carrier_id, name, billing_unit, eff_hi " +
                    "FROM Abfrage_Energietraeger_Effektiv WHERE ID_Projekt = ?",
                    new DbParam("@p", _idProjekt));

                foreach (DataRow r in (dt != null ? dt.Rows.Cast<DataRow>()
                                                  : Enumerable.Empty<DataRow>()))
                {
                    int carrier = (int)(D(r, "carrier_id") ?? 0);

                    // DER FILTER: nur Traeger, die im Projekt auch wirklich ein
                    // Gewerk faehrt (Befund 22.08.2026).
                    ProjektEnergietraegerCtrl.Verwendung v;
                    if (!verwendet.TryGetValue(carrier, out v)) continue;
                    angezeigt.Add(carrier);

                    double? preis, grund;
                    LiesPreise(carrier, out preis, out grund);
                    double? hi = D(r, "eff_hi");

                    bool ohnePreis = !preis.HasValue || Math.Abs(preis.Value) < 1e-9;
                    if (ohnePreis) _traegerOhnePreis.Add(S(r, "name"));

                    bool ohneHeizwert = !hi.HasValue || hi.Value <= 0;
                    if (ohneHeizwert && !ohnePreis) _traegerOhneHeizwert.Add(S(r, "name"));

                    EmissionsFaktorSatz faktoren = EmissionsFaktoren(carrier);

                    zeilen.Add(new TraegerZeile
                    {
                        TraegerId = carrier,
                        Zellen = new List<string>
                        {
                            S(r, "name"),
                            S(r, "billing_unit"),
                            hi.HasValue ? hi.Value.ToString("N2", kultur) : "—",
                            preis.HasValue ? preis.Value.ToString("N4", kultur) : "—",
                            (ohnePreis || ohneHeizwert) ? "—"
                                : (preis.Value / hi.Value).ToString("N4", kultur),
                            grund.HasValue ? grund.Value.ToString("N2", kultur) : "—",
                            LeistungspreisText(carrier, kultur),
                            Faktor(faktoren.Wirksam(emissionsModus), kultur),
                            Faktor(faktoren.So2, kultur),
                            Faktor(faktoren.Nox, kultur)
                        },
                        Kurztext = string.Format(MyResource.Resource.BK_KOSTEN_TRAEGER_HINT,
                                                 v.BeitraegerText),
                        // Die Herkunftsebene gehoert an die Zahl: 240 g/kWh aus
                        // der Projektuebersteuerung ist eine andere Aussage als
                        // 240 g/kWh aus dem Katalog.
                        EmissionKurztext = string.Format(
                            T("BK_KOSTEN_EMISSION_HINT",
                              "Emissionsfaktoren — CO₂ aus Ebene „{0}“, Berechnungsmodus {1}. " +
                              "Lesekette: Projektwert → aktiver Emissionswert → " +
                              "Brennstoff-Stamm → Trägerkatalog."),
                            faktoren.Co2Ebene, emissionsModus)
                    });
                }
            }
            catch { }

            FehlendeTraeger(verwendet, angezeigt, kultur, zeilen);

            if (zeilen.Count == 0) zeilen.Add(KeineTraeger(verwendet.Values));
            return zeilen;
        }

        /// <summary>
        /// Die rote Fehlzeile je verwendetem, aber nicht angezeigtem Träger
        /// (Anwenderentscheid 30.08.2026) — dasselbe Muster wie bei den
        /// Gewerken ohne Kostenposition.
        /// </summary>
        private static void FehlendeTraeger(
            Dictionary<int, ProjektEnergietraegerCtrl.Verwendung> verwendet,
            HashSet<int> angezeigt, CultureInfo kultur, List<TraegerZeile> ziel)
        {
            var fehlend = new List<ProjektEnergietraegerCtrl.Verwendung>();
            foreach (ProjektEnergietraegerCtrl.Verwendung v in verwendet.Values)
                if (!angezeigt.Contains(v.CarrierId)) fehlend.Add(v);
            if (fehlend.Count == 0) return;

            fehlend.Sort(delegate (ProjektEnergietraegerCtrl.Verwendung a,
                                   ProjektEnergietraegerCtrl.Verwendung b)
                         { return a.CarrierId.CompareTo(b.CarrierId); });

            foreach (ProjektEnergietraegerCtrl.Verwendung v in fehlend)
            {
                string name = v.Name.Length > 0 ? v.Name : "#" + v.CarrierId.ToString(kultur);
                var zellen = new List<string>
                {
                    string.Format(T("BK_KOSTEN_TRAEGER_FEHLZEILE", "{0} — nicht zugeordnet"), name)
                };
                for (int i = 1; i < 10; i++) zellen.Add("—");

                ziel.Add(new TraegerZeile
                {
                    TraegerId = v.CarrierId,
                    Art = ZeilenArt.OhnePosition,
                    Zellen = zellen,
                    Kurztext = string.Format(
                        T("BK_KOSTEN_TRAEGER_FEHLZEILE_HINT",
                          "„{0}“ wird von {1} verwendet, ist dem Projekt aber nicht " +
                          "zugeordnet — ohne Zuordnung gibt es weder Preis noch Heizwert " +
                          "noch Emissionsfaktoren, und die Energiekosten bleiben „—“. " +
                          "Zuordnen über den Knopf „{2}“ oben rechts."),
                        name, v.BeitraegerText,
                        T("BK_KOSTEN_BTN_TRAEGER", "Energieträgerverwaltung…"))
                });
            }
        }

        /// <summary>
        /// Eine erklärende Zeile statt eines leeren Rasters — ein leeres Raster
        /// sagt nicht, OB gefiltert wurde.
        /// </summary>
        private static TraegerZeile KeineTraeger(
            ICollection<ProjektEnergietraegerCtrl.Verwendung> verwendet)
        {
            var namen = new List<string>();
            foreach (ProjektEnergietraegerCtrl.Verwendung v in verwendet)
                namen.Add(v.Name.Length > 0 ? v.Name : "#" + v.CarrierId);

            string text = namen.Count > 0
                ? string.Format(MyResource.Resource.BK_KOSTEN_TRAEGER_UNGEPFLEGT,
                                string.Join(", ", namen.ToArray()))
                : MyResource.Resource.BK_KOSTEN_TRAEGER_KEINE;

            var zellen = new List<string> { text };
            for (int i = 1; i < 10; i++) zellen.Add("");
            return new TraegerZeile { Art = ZeilenArt.Hinweis, Zellen = zellen, Kurztext = text };
        }

        private EmissionsFaktorSatz EmissionsFaktoren(int carrierId)
        {
            try { return EmissionsFaktorLader.Lade(_idProjekt, carrierId); }
            catch { return new EmissionsFaktorSatz(); }
        }

        private static string Faktor(double? wert, CultureInfo kultur)
        {
            return wert.HasValue ? wert.Value.ToString("N2", kultur) : "—";
        }

        /// <summary>
        /// KD6 (§ 10): der effektive Leistungspreis als JAHRESWERT
        /// [€/(kW·a)] — <c>custom_price_power</c> vor <c>price_power</c>
        /// (0 = nicht gepflegt), Monatsmodus × 12.
        /// </summary>
        private string LeistungspreisText(int carrierId, CultureInfo kultur)
        {
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_power, price_power_modus, pricing_model " +
                    "FROM energy_carrier WHERE id = ?",
                    new DbParam("@c", carrierId));
                if (k == null || k.Rows.Count == 0) return "—";

                double? satz = null;
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_power FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", _idProjekt), new DbParam("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    double? cw = D(s.Rows[0], "custom_price_power");
                    if (cw.HasValue && cw.Value > 0) satz = cw;
                }
                if (!satz.HasValue)
                {
                    double? kw = D(k.Rows[0], "price_power");
                    if (kw.HasValue && kw.Value > 0) satz = kw;
                }
                if (!satz.HasValue) return "—";

                bool monat = string.Equals(S(k.Rows[0], "price_power_modus"),
                    DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal);
                return (monat ? satz.Value * 12.0 : satz.Value).ToString("N2", kultur);
            }
            catch { return "—"; }
        }

        /// <summary>
        /// Arbeits- und Grundpreis — dieselbe Vorrangkette wie
        /// <c>KostenEmissionRechner.LadeTraeger</c> (Ä-BK3). Ein Arbeitspreis
        /// von 0 zählt als NICHT GEPFLEGT (Befund D5); der Grundpreis behält
        /// „Projektwert vor Katalogwert" samt der 0.
        /// </summary>
        private void LiesPreise(int carrierId, out double? arbeit, out double? grund)
        {
            arbeit = null; grund = null;
            if (carrierId <= 0) return;

            double? sArbeit = null, sGrund = null;
            try
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_work, custom_price_base FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", _idProjekt), new DbParam("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    sArbeit = D(s.Rows[0], "custom_price_work");
                    sGrund = D(s.Rows[0], "custom_price_base");
                }
            }
            catch { }

            double? kArbeit = null, kGrund = null;
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_work, price_base FROM energy_carrier WHERE id = ?",
                    new DbParam("@c", carrierId));
                if (k != null && k.Rows.Count > 0)
                {
                    kArbeit = D(k.Rows[0], "price_work");
                    kGrund = D(k.Rows[0], "price_base");
                }
            }
            catch { }

            arbeit = (sArbeit.HasValue && sArbeit.Value > 0) ? sArbeit
                   : ((kArbeit.HasValue && kArbeit.Value > 0) ? kArbeit : null);
            grund = sGrund ?? kGrund;
        }

        // =====================================================================
        // Die zwei Einstiege und das Löschen
        // =====================================================================

        /// <summary>
        /// KD6a (§ 3.2): Der Einstieg führt in die Kostenverwaltung im
        /// Projektmodus. Ä19: vorgewählt wird die Komponente der GEWÄHLTEN
        /// Anlagenzeile.
        /// </summary>
        private IReadOnlyDictionary<string, object> VerwaltungGaben(KostenZeile zeile)
        {
            if (_idProjekt <= 0) return null;

            ProjektEnergietraegerCtrl.AnlagenEintrag a = null;
            if (zeile != null) _anlagen.TryGetValue(zeile.Schluessel, out a);

            return KostenKomponenteHuelle.GabenProjekt(_idProjekt, _projektname,
                                                       a != null ? a.Komponente : null,
                                                       false, a != null ? a.AnlageId : 0);
        }

        /// <summary>KD6a: die Energieträgerverwaltung, vorgefiltert auf das Projekt.</summary>
        private IReadOnlyDictionary<string, object> TraegerGaben()
        {
            if (_idProjekt <= 0) return null;
            return EnergietraegerHuelle.Gaben(_idProjekt);
        }

        /// <summary>Ä21: die Frage vor dem Löschen der losen Positionen.</summary>
        private string LoeschFrage(KostenZeile zeile)
        {
            string komponente;
            if (zeile == null || !_loseKomponenten.TryGetValue(zeile.Schluessel, out komponente))
                return "";

            return string.Format(T("BK_KOSTEN_LOSE_LOESCHEN",
                "Alle Kostenpositionen ohne Anlagenzuordnung der Komponente „{0}“ " +
                "löschen?\n\nSie stammen z. B. aus einer Variantenkopie ohne dieses " +
                "Gewerk und rechnen bis dahin in der Wirtschaftlichkeit mit."), komponente);
        }

        private string Loeschen(KostenZeile zeile)
        {
            string komponente;
            if (zeile == null || !_loseKomponenten.TryGetValue(zeile.Schluessel, out komponente))
                return "";

            object kid = null;
            try
            {
                kid = DataRepository.ExecuteScalar(
                    "SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new DbParam("@k", komponente));
            }
            catch { }
            if (kid == null || kid == DBNull.Value) return "";

            int n = KostenProjektPositionenCtrl.LoseLoeschen(_idProjekt, Convert.ToInt32(kid));
            return string.Format(T("BK_KOSTEN_LOSE_GELOESCHT",
                "{0} Position(en) der Komponente „{1}“ gelöscht."), n, komponente);
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        private static string S(DataRow r, string col)
        {
            return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? r[col].ToString() : "";
        }

        private static double? D(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[col]); } catch { return null; }
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
