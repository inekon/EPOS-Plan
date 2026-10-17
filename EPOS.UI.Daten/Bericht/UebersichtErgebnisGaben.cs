using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Berichte;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Hülle des Abschnitts „Ergebnisse — Energie“ der Übersicht (Anwenderauftrag 13.10.2026): baut aus dem Stand der
    /// Seite (Gruppe, Vergleichswahl, Stamm) den Energievergleich des Kerns (<see cref="EnergievergleichCtrl"/>,
    /// <see cref="Energievergleich"/>) und bildet ihn auf die Typen der Razor-Komponente <c>UebersichtErgebnisse</c> ab.
    /// Die Kennzahlen je Version werden über die Sitzung gepuffert, solange sich ihr Simulationsstand nicht ändert.
    ///
    /// <para><b>Anzeige anpassen (Anwenderentscheid 13.10.2026).</b> Die Auflösung steht hier, an einer Stelle: Der
    /// Zustand einer Gruppe ist der Benutzerzustand aus <see cref="UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN"/>,
    /// sonst die Vorgabe des Kerns (<see cref="Energievergleich.GRUPPEN_DATEN"/>); abgewählte Kennzahlen
    /// (<see cref="UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS"/>) und unter
    /// <see cref="UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS"/> die Zeilen ohne Wert nimmt die reine Funktion
    /// <see cref="UebersichtErgebnisAnzeige.Anwenden"/> aus der Gruppenliste — so gilt die Einstellung für Tabelle und
    /// Grafik gleich. Ob eine zugeklappte Gruppe gerade aufgeklappt ist, entscheidet allein die Sitzungsablage
    /// (<c>KlappgruppeOffenLesen</c>); gespeichert wird nur der Grundzustand, je Benutzer über
    /// <c>Dienste.Einstellungen</c>, für alle Projekte gleich.</para>
    /// </summary>
    internal sealed class UebersichtErgebnisGaben
    {
        /// <summary>Höchstens so viele Variantenspalten wie die Gegenüberstellung der Komponenten.</summary>
        internal const int MAX_VARIANTENSPALTEN = 8;

        /// <summary>
        /// Die aufgeklappten Klappgruppen (Gruppenschlüssel des Kerns) — ein Stand je Gruppe und Sitzung, wie „nur
        /// Unterschiede“.
        /// </summary>
        private static readonly HashSet<string> s_offeneKlappgruppen = new HashSet<string>();

        /// <summary>
        /// Die gewählte Version des Energieflussbilds (Projekt-Id) — je Sitzung wie die Klappgruppen, nicht je Benutzer
        /// (Konzept Energieflussbild 4.2); <c>null</c> = Vorgabe Stamm.
        /// </summary>
        private static int? s_flussVersion;

        /// <summary>
        /// Der Formfilter des Energieflussbilds („alle“, „strom“, „waerme“, „kaelte“; <see cref="ErgebnisFlussWahl"/>) — je
        /// Sitzung wie die Versionswahl, nicht je Benutzer.
        /// </summary>
        private static string s_flussFilter = ErgebnisFlussWahl.ALLE;

        /// <summary>
        /// Der Einstellungsschlüssel der Darstellung des Energieflussbilds („kette“ | „stufen“ | „zuordnung“, SK-Q17) — je Benutzer über die
        /// Naht des Kerns wie die Ansicht; ein unbekannter Wert gilt als Kette.
        /// </summary>
        internal const string EINSTELLUNG_FLUSS_DARSTELLUNG = "Uebersicht.Ergebnisse.Fluss.Darstellung";

        /// <summary>Der Einstellungsschlüssel der Ansicht „Tabelle | Grafik“ — je Benutzer über die Naht des Kerns.</summary>
        internal const string EINSTELLUNG_ANSICHT = "Uebersicht.Ergebnisse.Ansicht";

        private readonly Func<int, bool, VariantenDaten> _lade;
        private readonly Dictionary<int, (string Stand, VariantenDaten Daten)> _puffer =
            new Dictionary<int, (string, VariantenDaten)>();

        private readonly IEinstellungen _einstellungen;

        /// <summary>Die Versionen des zuletzt geladenen Standes je Projekt-Id (Zeile und Spaltenname) — der Weg der Versionswahl.</summary>
        private readonly Dictionary<int, (VarianteZeile Zeile, string Name)> _flussVersionen =
            new Dictionary<int, (VarianteZeile, string)>();

        /// <summary>Das Energieflussbild je Version, solange ihr Simulationsstand und die Sprache dieselben sind.</summary>
        private readonly Dictionary<int, (string Stand, bool Englisch, string Name, ErgebnisFlussStand Fluss)> _flussPuffer =
            new Dictionary<int, (string, bool, string, ErgebnisFlussStand)>();

        /// <summary>Mit dem Ladeweg des Kerns.</summary>
        internal UebersichtErgebnisGaben() : this(EnergievergleichCtrl.Lade) { }

        /// <summary>
        /// Mit einem eigenen Ladeweg (Proben) und wahlweise einer eigenen Einstellungsablage (Attrappe der Naht);
        /// ohne sie gilt <c>Dienste.Einstellungen</c> zum Zeitpunkt des Zugriffs.
        /// </summary>
        internal UebersichtErgebnisGaben(Func<int, bool, VariantenDaten> lade, IEinstellungen einstellungen = null)
        {
            _lade = lade ?? throw new ArgumentNullException(nameof(lade));
            _einstellungen = einstellungen;
        }

        private IEinstellungen Ablage => _einstellungen ?? Dienste.Einstellungen;

        /// <summary>Der Parametersatz der Komponente — ohne <c>Stand</c>, den reicht die Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<UebersichtStand, UebersichtErgebnisStand>(Laden),
                ["Titel"] = MyResource.Resource.BK_ERG_ABSCHNITT_ERGEBNISSE,
                ["SpalteKennzahl"] = MyResource.Resource.BK_ERG_SP_KENNZAHL,
                ["LegendeAbweichung"] = MyResource.Resource.BK_LBL_LEGENDE_ABWEICHUNG,
                ["LegendeProzent"] = MyResource.Resource.BK_ERG_LEGENDE_PROZENT,
                ["HinweisQuelle"] = MyResource.Resource.BK_ERG_HINWEIS_QUELLE,
                ["KeineSimulationText"] = MyResource.Resource.BK_ERG_KEINE_SIMULATION,
                ["NichtSimuliertText"] = MyResource.Resource.BK_LBL_NICHT_SIMULIERT,
                ["WeichtAbText"] = MyResource.Resource.BK_LBL_WEICHT_AB,
                ["KeineUnterschiedeText"] = MyResource.Resource.BK_ERG_KEINE_UNTERSCHIEDE,
                ["AnzahlFormat"] = MyResource.Resource.BK_ERG_ANZAHL_KENNZAHLEN,
                ["AnzahlEins"] = MyResource.Resource.BK_ERG_ANZAHL_KENNZAHL_EINS,
                ["AnzeigeLesen"] = new Func<ErgebnisAnzeigeEinstellung>(AnzeigeLesen),
                ["AnzeigeSpeichern"] = new Action<ErgebnisAnzeigeEinstellung>(AnzeigeSpeichern),
                ["AnpassenTexte"] = AnpassenTexte(),
                ["HilfeSchluessel"] = "UcBkUebersicht.btn_Help",
                ["AnsichtText"] = MyResource.Resource.BK_ERG_ANSICHT,
                ["AnsichtTabelleText"] = MyResource.Resource.BK_ERG_ANSICHT_TABELLE,
                ["AnsichtGrafikText"] = MyResource.Resource.BK_ERG_ANSICHT_GRAFIK,
                ["FlussTexte"] = FlussTexte(),
                ["FlussLaden"] = new Func<int, ErgebnisFlussStand>(FlussLaden),
                ["FlussVersionLesen"] = new Func<int?>(() => s_flussVersion),
                ["FlussVersionGewechselt"] = new Action<int>(id => s_flussVersion = id),
                ["FlussDarstellungLesen"] = new Func<string>(() => FlussDarstellungLesen(Ablage)),
                ["FlussDarstellungGewechselt"] = new Action<string>(d => FlussDarstellungSchreiben(d, Ablage)),
                ["FlussFilterLesen"] = new Func<string>(() => s_flussFilter),
                ["FlussFilterGewechselt"] = new Action<string>(f => s_flussFilter = ErgebnisFlussWahl.Filter(f)),
                ["GrafikLegendeText"] = MyResource.Resource.BK_ERG_GRAFIK_LEGENDE,
                ["GrafikHinweisText"] = MyResource.Resource.BK_ERG_GRAFIK_HINWEIS,
                ["AnsichtLesen"] = new Func<string>(() => AnsichtLesen(Ablage)),
                ["AnsichtGewechselt"] = new Action<string>(a => AnsichtSchreiben(a, Ablage)),
                ["KlappgruppeOffenLesen"] = new Func<string, bool>(g => s_offeneKlappgruppen.Contains(g)),
                ["KlappgruppeOffenGewechselt"] = new Action<string, bool>((g, an) =>
                {
                    if (an) s_offeneKlappgruppen.Add(g);
                    else s_offeneKlappgruppen.Remove(g);
                })
            };
        }

        /// <summary>
        /// Die gewählte Ansicht aus derselben Ablage wie die Anzeigeeinstellung (<paramref name="ablage"/>, sonst
        /// <c>Dienste.Einstellungen</c>); ohne gemerkte Wahl oder bei einem Fehler <c>null</c>, dann gilt die Vorgabe
        /// der Komponente (das Energieflussbild).
        /// </summary>
        internal static string AnsichtLesen(IEinstellungen ablage = null)
        {
            try { return (ablage ?? Dienste.Einstellungen).Lies(EINSTELLUNG_ANSICHT, null); }
            catch (Exception) { return null; }
        }

        /// <summary>Die gemerkte Darstellung des Energieflussbilds; ohne Ablage, bei einem Fehler oder unbekanntem Wert die Kette.</summary>
        internal static string FlussDarstellungLesen(IEinstellungen ablage = null)
        {
            try { return ErgebnisFlussWahl.Darstellung((ablage ?? Dienste.Einstellungen).Lies(EINSTELLUNG_FLUSS_DARSTELLUNG, ErgebnisFlussWahl.KETTE)); }
            catch (Exception) { return ErgebnisFlussWahl.KETTE; }
        }

        /// <summary>Legt die Darstellung je Benutzer ab; ein Fehler der Ablage lässt die Wahl für diese Sitzung stehen.</summary>
        internal static void FlussDarstellungSchreiben(string darstellung, IEinstellungen ablage = null)
        {
            try { (ablage ?? Dienste.Einstellungen).Schreib(EINSTELLUNG_FLUSS_DARSTELLUNG, ErgebnisFlussWahl.Darstellung(darstellung)); }
            catch (Exception) { /* Die Wahl gilt dann nur für diese Sitzung. */ }
        }

        /// <summary>Legt die gewählte Ansicht je Benutzer ab; ein Fehler der Ablage lässt die Anzeige stehen.</summary>
        internal static void AnsichtSchreiben(string ansicht, IEinstellungen ablage = null)
        {
            try { (ablage ?? Dienste.Einstellungen).Schreib(EINSTELLUNG_ANSICHT, ansicht ?? UebersichtErgebnisse.ANSICHT_TABELLE); }
            catch (Exception) { /* Die Wahl gilt dann nur für diese Sitzung. */ }
        }

        /// <summary>
        /// Der Energievergleich zum Stand der Seite: der Stamm und die Varianten der Vergleichswahl in der Reihenfolge der
        /// Gruppe, höchstens <see cref="MAX_VARIANTENSPALTEN"/> Varianten — dieselben Spalten wie die Gegenüberstellung.
        /// </summary>
        internal UebersichtErgebnisStand Laden(UebersichtStand stand)
        {
            var erg = new UebersichtErgebnisStand();
            VarianteZeile stamm = stand?.Zeilen.FirstOrDefault(z => z.IstStamm);
            if (stamm == null) return erg;

            List<VarianteZeile> versionen = new List<VarianteZeile> { stamm };
            versionen.AddRange(stand.Zeilen
                .Where(z => !z.IstStamm && stand.GewaehlteVarianten.Contains(z.IdProjekt))
                .Take(MAX_VARIANTENSPALTEN));

            erg.Versionsspalten = versionen.Select(v => new VersionsSpalte
            {
                Name = v.IstStamm ? MyResource.Resource.BK_SP_WERT_STAMM
                                  : (string.IsNullOrEmpty(v.Bezeichner) ? v.Projektname : v.Bezeichner),
                Stand = string.IsNullOrEmpty(v.SimZeitpunkt)
                    ? MyResource.Resource.BK_LBL_NICHT_SIMULIERT
                    : string.Format(MyResource.Resource.BK_LBL_SIMULIERT_AM, v.SimZeitpunkt)
            }).ToList();

            _flussVersionen.Clear();
            for (int i = 0; i < versionen.Count; i++) _flussVersionen[versionen[i].IdProjekt] = (versionen[i], erg.Versionsspalten[i].Name);
            erg.FlussVersionen = versionen.Select((v, i) => new ErgebnisFlussVersion
            {
                IdProjekt = v.IdProjekt,
                Name = erg.Versionsspalten[i].Name,
                Simuliert = !string.IsNullOrEmpty(v.SimZeitpunkt)
            }).ToList();

            List<VariantenDaten> daten = versionen.Select(Daten).ToList();
            EnergievergleichErgebnis vergleich = Energievergleich.Vergleiche(daten, Sprache.Englisch, CultureInfo.CurrentCulture);
            erg.KeineSimulation = vergleich.KeineSimuliert;
            erg.Simuliert = vergleich.Simuliert.ToList();
            ErgebnisAnzeigeAuswertung anzeige = UebersichtErgebnisAnzeige.Anwenden(Abbilden(vergleich), AnzeigeLesen());
            erg.Gruppen = anzeige.Gruppen;
            erg.OhneWertAusgeblendet = anzeige.OhneWertAusgeblendet;
            string modus = EmissionsAusweis.ModusAusVarianten(daten.Where(v => v != null && v.Ergebnis != null));
            erg.Anpassbar = Anpassbar(vergleich, modus);
            return erg;
        }

        // ---- Energieflussbild (Konzept Energieflussbild, Abschnitt 4) ---------------------------------------------

        /// <summary>Die Beschriftungen der Ansicht „Fluss“ aus den Ressourcen.</summary>
        internal static ErgebnisFlussTexte FlussTexte() => new ErgebnisFlussTexte
        {
            Ansicht = MyResource.Resource.BK_ERG_ANSICHT_FLUSS,
            Version = MyResource.Resource.BK_ERG_FLUSS_VERSION,
            NichtSimuliertZusatz = MyResource.Resource.BK_LBL_NICHT_SIMULIERT,
            NichtSimuliert = MyResource.Resource.BK_ERG_FLUSS_NICHT_SIMULIERT,
            Bild = MyResource.Resource.BK_ERG_FLUSS_BILD,
            Zeile = MyResource.Resource.BK_ERG_FLUSS_ZEILE,
            Knoten = MyResource.Resource.BK_ERG_FLUSS_KNOTEN,
            TextFluesse = MyResource.Resource.BK_ERG_FLUSS_TEXT_FLUESSE,
            TextKnoten = MyResource.Resource.BK_ERG_FLUSS_TEXT_KNOTEN,
            Zeiger = MyResource.Resource.BK_ERG_FLUSS_ZEIGER,
            Darstellung = MyResource.Resource.BK_ERG_FLUSS_DARSTELLUNG,
            Filter = MyResource.Resource.BK_ERG_FLUSS_FILTER,
            Darstellungen = ErgebnisFlussWahl.DARSTELLUNGEN.ToDictionary(d => d,
                d => EnergieflussAbbildung.Darstellungstext(KernDarstellung(d), Sprache.Englisch), StringComparer.Ordinal),
            Filterknoepfe = ErgebnisFlussWahl.FILTER.ToDictionary(f => f,
                f => EnergieflussAbbildung.Filtertext(KernFilter(f), Sprache.Englisch), StringComparer.Ordinal)
        };

        /// <summary>Die Darstellung des Kerns zu einem Ablagewert (<see cref="ErgebnisFlussWahl"/>).</summary>
        internal static EnergieflussDarstellung KernDarstellung(string wert)
            => ErgebnisFlussWahl.Darstellung(wert) switch
            {
                ErgebnisFlussWahl.STUFEN => EnergieflussDarstellung.Stufen,
                ErgebnisFlussWahl.ZUORDNUNG => EnergieflussDarstellung.Zuordnung,
                _ => EnergieflussDarstellung.Kette
            };

        /// <summary>Der Formfilter des Kerns zu einem Ablagewert (<see cref="ErgebnisFlussWahl"/>).</summary>
        internal static EnergieflussFilter KernFilter(string wert)
            => ErgebnisFlussWahl.Filter(wert) switch
            {
                ErgebnisFlussWahl.STROM => EnergieflussFilter.Strom,
                ErgebnisFlussWahl.WAERME => EnergieflussFilter.Waerme,
                ErgebnisFlussWahl.KAELTE => EnergieflussFilter.Kaelte,
                _ => EnergieflussFilter.Alle
            };

        /// <summary>
        /// Das Energieflussbild der Version <paramref name="idProjekt"/> des zuletzt geladenen Standes: Kennzahlen aus
        /// demselben Puffer wie die Tabelle → <see cref="Energiefluss.Aus(VariantenDaten, bool, string)"/> (Bilanz) →
        /// <see cref="EnergieflussAbbildung.Bild"/> → <see cref="ChartRenderer.EnergieflussModell"/> breit und schmal, ohne
        /// Knotenwerte (SK-Q12: Werte nur im Zeigertext). Je Version, Simulationsstand und Sprache zwischengespeichert —
        /// dieselbe Referenz, solange sich nichts ändert, damit die Komponente ihren Baum nicht neu baut. Eine Version außerhalb
        /// des Standes oder ohne Lauf liefert einen benannten Stand ohne Bild.
        /// </summary>
        internal ErgebnisFlussStand FlussLaden(int idProjekt)
        {
            if (!_flussVersionen.TryGetValue(idProjekt, out var eintrag))
                return new ErgebnisFlussStand { IdProjekt = idProjekt, NichtSimuliert = true };
            VarianteZeile v = eintrag.Zeile;
            string stand = v.SimZeitpunkt ?? "";
            bool englisch = Sprache.Englisch;
            if (_flussPuffer.TryGetValue(idProjekt, out var p) && p.Stand == stand && p.Englisch == englisch && p.Name == eintrag.Name)
                return p.Fluss;

            ErgebnisFlussStand fluss;
            if (stand.Length == 0)
                fluss = new ErgebnisFlussStand { IdProjekt = idProjekt, Version = eintrag.Name, NichtSimuliert = true };
            else
            {
                VariantenDaten vd = Daten(v);
                EnergieflussBilanz b = Energiefluss.Aus(vd, englisch, eintrag.Name);
                fluss = FlussStand(b, englisch, () => Energiefluss.Aus(vd, englisch, eintrag.Name, EnergieflussDarstellung.Zuordnung));
            }
            _flussPuffer[idProjekt] = (stand, englisch, eintrag.Name, fluss);
            return fluss;
        }

        /// <summary>
        /// Bildet eine Bilanz des Kerns auf den Stand der Komponente ab (reine Funktion, auch für Proben). Die Darstellung
        /// Zuordnung zeichnet aus einer eigenen Bilanz (<paramref name="zuordnung"/>, gerufen erst beim ersten Bedarf); ohne
        /// sie zeigt die Zuordnung das Bild der Kette.
        /// </summary>
        internal static ErgebnisFlussStand FlussStand(EnergieflussBilanz b, bool englisch, Func<EnergieflussBilanz> zuordnung = null)
        {
            if (b == null) return new ErgebnisFlussStand { NichtSimuliert = true };
            if (b.Leer)
                return new ErgebnisFlussStand
                {
                    IdProjekt = b.IdProjekt, Version = b.Version, NichtSimuliert = !b.Simuliert, Grund = b.Grund ?? "",
                    KiText = b.KiText().Replace("\r\n", "\n").TrimEnd()
                };

            CultureInfo k = Energiefluss.Kultur(englisch);
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(b);

            var hinweise = new List<string>(b.Hinweise);
            foreach (EnergieflussKnoten kn in b.Knoten.Where(x => x.Gebuendelt.Count > 0))
                hinweise.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BK_ERG_FLUSS_HINWEIS_GEBUENDELT, kn.Name, kn.Gebuendelt.Count));
            if (b.Knoten.Any(x => x.Id == Energiefluss.UMWELTWAERME || x.Id == Energiefluss.UMWELTKAELTE))
                hinweise.Add(MyResource.Resource.BK_ERG_FLUSS_HINWEIS_UMWELT);

            // Die übrigen Bilder (Stufen, Formfilter; Konzept 3.5) entstehen beim ersten Bedarf und bleiben im Stand,
            // der selbst je Version und Simulationsstand im Puffer liegt — nie je Rendern.
            // Die Zuordnung hat eine eigene Bilanz und damit eigene Kanten und eine eigene Textalternative (einmal je Stand).
            EnergieflussBilanz bz = null;
            EnergieflussBildDaten dz = null;
            ErgebnisFlussBild Bildbau(string darstellung, string filter)
            {
                EnergieflussDarstellung art = KernDarstellung(darstellung);
                bool eigen = art == EnergieflussDarstellung.Zuordnung && zuordnung != null;
                if (eigen && dz == null)
                {
                    bz = zuordnung() ?? b;
                    dz = bz.Leer ? d : EnergieflussAbbildung.Bild(bz);
                }
                EnergieflussBildDaten df = EnergieflussAbbildung.Gefiltert(eigen ? dz : d, KernFilter(filter), englisch);
                return new ErgebnisFlussBild
                {
                    Breit = ChartRenderer.EnergieflussModell(df, ChartRenderer.ENERGIEFLUSS_BREIT, false, art),
                    Schmal = ChartRenderer.EnergieflussModell(df, ChartRenderer.ENERGIEFLUSS_SCHMAL, false, art),
                    Hinweis = df.Hinweis ?? "",
                    KantenJson = eigen ? KantenJson(dz) : null,
                    BusseJson = eigen ? BusseJson(dz) : null,
                    Fluesse = eigen && !bz.Leer ? Flusszeilen(bz, englisch) : null,
                    Knoten = eigen && !bz.Leer ? Knotenzeilen(bz, englisch) : null
                };
            }

            return new ErgebnisFlussStand
            {
                IdProjekt = b.IdProjekt,
                Version = b.Version,
                Breit = ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_BREIT, knotenwerte: false),
                Schmal = ChartRenderer.EnergieflussModell(d, ChartRenderer.ENERGIEFLUSS_SCHMAL, knotenwerte: false),
                Bildbau = Bildbau,
                KantenJson = KantenJson(d),
                BusseJson = BusseJson(d),
                Fluesse = Flusszeilen(b, englisch),
                Knoten = Knotenzeilen(b, englisch),
                Hinweise = hinweise,
                KiText = b.KiText().Replace("\r\n", "\n").TrimEnd()
            };
        }

        /// <summary>Je Band einer Bilanz eine Zeile der Textalternative.</summary>
        private static List<ErgebnisFlussZeile> Flusszeilen(EnergieflussBilanz b, bool englisch)
        {
            CultureInfo k = Energiefluss.Kultur(englisch);
            string Name(string id) => b.KnotenMit(id)?.Name ?? id;
            return b.Fluesse.Select(f => new ErgebnisFlussZeile
            {
                Von = Name(f.Von),
                Nach = Name(f.Nach),
                Wert = Energiefluss.Zahl(f.Mwh, k),
                Anteil = Energiefluss.Prozent(f.AnteilProzent, k),
                Herkunft = Energiefluss.HerkunftText(f.Herkunft, englisch)
            }).ToList();
        }

        /// <summary>Je Knoten einer Bilanz mit Fluss seine Bilanzzeile der Textalternative.</summary>
        private static List<ErgebnisFlussKnotenzeile> Knotenzeilen(EnergieflussBilanz b, bool englisch)
        {
            CultureInfo k = Energiefluss.Kultur(englisch);
            return b.Knoten.Where(x => x.ZuflussMwh != 0 || x.AbflussMwh != 0).Select(x => new ErgebnisFlussKnotenzeile
            {
                Name = x.Name,
                Zufluss = Energiefluss.Zahl(x.ZuflussMwh, k),
                Abfluss = Energiefluss.Zahl(x.AbflussMwh, k),
                Rest = Energiefluss.Zahl(Math.Abs(x.RestMwh) < 0.005 ? 0.0 : x.RestMwh, k)
            }).ToList();
        }

        /// <summary>
        /// Die Kanten der Hervorhebung aus den ungefilterten Bilddaten (Konzept 3.3): je Band Marke
        /// <c>"fluss:" + Von + ":" + Nach</c>, Von, Nach — als JSON, damit das Modul die Marke nicht zerlegen muss.
        /// </summary>
        internal static string KantenJson(EnergieflussBildDaten d)
        {
            // Wie die Referenz EnergieflussAbbildung.Umfeld: nur Bänder zwischen zwei bekannten, verschiedenen Knoten.
            var bekannt = new HashSet<string>((d?.Knoten ?? Array.Empty<EnergieflussBildKnoten>())
                .Where(k => k != null && !string.IsNullOrEmpty(k.Kennung)).Select(k => k.Kennung), StringComparer.Ordinal);
            return System.Text.Json.JsonSerializer.Serialize(
                (d?.Baender ?? Array.Empty<EnergieflussBildBand>())
                    .Where(x => x != null && x.Von != null && x.Nach != null && x.Von != x.Nach
                                && bekannt.Contains(x.Von) && bekannt.Contains(x.Nach))
                    .Select(x => new[] { "fluss:" + x.Von + ":" + x.Nach, x.Von, x.Nach }));
        }

        /// <summary>Die Kennungen der Busse aus den ungefilterten Bilddaten als JSON — ein Bus beendet den Weg der Hervorhebung.</summary>
        internal static string BusseJson(EnergieflussBildDaten d)
            => System.Text.Json.JsonSerializer.Serialize(
                (d?.Knoten ?? Array.Empty<EnergieflussBildKnoten>())
                    .Where(k => k != null && k.Art == EnergieflussKnotenart.Bus && !string.IsNullOrEmpty(k.Kennung))
                    .Select(k => k.Kennung).Distinct(StringComparer.Ordinal));

        // ---- Anzeige anpassen -------------------------------------------------------------------------------------

        /// <summary>Die Vorgabe des Kerns je Gruppe als Zustand.</summary>
        internal static IReadOnlyDictionary<string, ErgebnisGruppenZustand> Vorgaben()
            => Energievergleich.GRUPPEN_DATEN.ToDictionary(g => g.Schluessel,
                g => g.VorgabeOffen ? ErgebnisGruppenZustand.Offen : ErgebnisGruppenZustand.Zugeklappt, StringComparer.Ordinal);

        /// <summary>Die gespeicherte Einstellung des Benutzers; die Vorgabe, wenn nichts hinterlegt oder nichts lesbar ist.</summary>
        internal ErgebnisAnzeigeEinstellung AnzeigeLesen()
        {
            try
            {
                IEinstellungen a = Ablage;
                if (a == null) return ErgebnisAnzeigeEinstellung.Standard();
                return UebersichtErgebnisAnzeige.AusText(a.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN, ""),
                                                         a.Lies(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS, ""),
                                                         a.LiesZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, 1),
                                                         Vorgaben());
            }
            catch
            {
                return ErgebnisAnzeigeEinstellung.Standard();
            }
        }

        /// <summary>
        /// Schreibt den Grundzustand aus dem Dialog (nach OK). Gleicht er der Vorgabe, verschwinden die drei Schlüssel.
        /// Eine Ablage, die nicht schreiben kann, lässt die Anzeige beim alten Stand — kein Abbruch.
        /// </summary>
        internal void AnzeigeSpeichern(ErgebnisAnzeigeEinstellung e)
        {
            if (e == null) return;
            try
            {
                IEinstellungen a = Ablage;
                if (a == null) return;
                if (e.IstStandard)
                {
                    a.Loesche(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN);
                    a.Loesche(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS);
                    a.Loesche(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS);
                    return;
                }
                a.Schreib(UebersichtErgebnisAnzeige.SCHLUESSEL_GRUPPEN, UebersichtErgebnisAnzeige.GruppenText(e));
                a.Schreib(UebersichtErgebnisAnzeige.SCHLUESSEL_KENNZAHLEN_AUS, UebersichtErgebnisAnzeige.KennzahlenText(e));
                a.SchreibZahl(UebersichtErgebnisAnzeige.SCHLUESSEL_OHNE_WERT_AUS, e.OhneWertAus ? 1 : 0);
            }
            catch
            {
                // Die Einstellung ist eine Bequemlichkeit; ein Schreibfehler der Ablage hält die Seite nicht an.
            }
        }

        /// <summary>
        /// Die Gruppen des Dialogs in Anzeigereihenfolge: je Gruppe die Kennzahlen des Katalogs (Modus wie der Vergleich)
        /// und die Trägerzeilen der aktuellen Daten.
        /// </summary>
        internal static IReadOnlyList<ErgebnisAnpassGruppe> Anpassbar(EnergievergleichErgebnis vergleich, string modus)
        {
            var jeGruppe = Energievergleich.GRUPPEN.ToDictionary(g => g, g => new List<ErgebnisAnpassKennzahl>());
            var bekannt = new HashSet<string>(StringComparer.Ordinal);
            foreach (Kennzahl k in Energievergleich.Kennzahlen(modus))
            {
                if (!jeGruppe.TryGetValue(Energievergleich.Gruppe(k), out var liste) || !bekannt.Add(k.Schluessel)) continue;
                string einheit = k.Einheit == "–" ? "" : (k.Einheit ?? "");
                string text = k.Label(Sprache.Englisch);
                liste.Add(new ErgebnisAnpassKennzahl { Schluessel = k.Schluessel, Bezeichnung = einheit.Length == 0 ? text : text + " [" + einheit + "]" });
            }
            foreach (EnergievergleichZeile z in vergleich?.Zeilen ?? new List<EnergievergleichZeile>())
            {
                if (!jeGruppe.TryGetValue(z.Gruppe, out var liste) || !bekannt.Add(z.Schluessel)) continue;
                liste.Add(new ErgebnisAnpassKennzahl
                {
                    Schluessel = z.Schluessel,
                    Bezeichnung = z.Einheit.Length == 0 ? z.Bezeichnung : z.Bezeichnung + " [" + z.Einheit + "]"
                });
            }
            return Energievergleich.GRUPPEN_DATEN.Select(g => new ErgebnisAnpassGruppe
            {
                Schluessel = g.Schluessel,
                Titel = Gruppentitel(g.Schluessel),
                Vorgabe = g.VorgabeOffen ? ErgebnisGruppenZustand.Offen : ErgebnisGruppenZustand.Zugeklappt,
                Kennzahlen = jeGruppe[g.Schluessel]
            }).ToList();
        }

        /// <summary>Die Beschriftungen des Dialogs „Anzeige anpassen…“ aus den Ressourcen.</summary>
        internal static ErgebnisAnpassenTexte AnpassenTexte() => new ErgebnisAnpassenTexte
        {
            Knopf = MyResource.Resource.BK_ERG_ANP_KNOPF,
            Titel = MyResource.Resource.BK_ERG_ANP_TITEL,
            Hinweis = MyResource.Resource.BK_ERG_ANP_HINWEIS,
            Offen = MyResource.Resource.BK_ERG_ANP_OFFEN,
            Zugeklappt = MyResource.Resource.BK_ERG_ANP_ZUGEKLAPPT,
            Ausgeblendet = MyResource.Resource.BK_ERG_ANP_AUSGEBLENDET,
            Kennzahlen = MyResource.Resource.BK_ERG_ANP_KENNZAHLEN,
            KennzahlenAnzahl = MyResource.Resource.BK_ERG_ANP_KENNZAHLEN_ANZAHL,
            OhneWert = MyResource.Resource.BK_ERG_ANP_OHNE_WERT,
            OhneWertErklaerung = MyResource.Resource.BK_ERG_ANP_OHNE_WERT_ERKL,
            Standard = MyResource.Resource.BK_ERG_ANP_STANDARD,
            OhneWertHinweis = MyResource.Resource.BK_ERG_ANP_OHNE_WERT_HINWEIS,
            OhneWertHinweisEins = MyResource.Resource.BK_ERG_ANP_OHNE_WERT_HINWEIS_EINS,
            Ok = MyResource.Resource.SIM_BTN_OK,
            Abbrechen = MyResource.Resource.SIM_BTN_ABBRECHEN
        };

        /// <summary>Die Kennzahlen einer Version — aus dem Puffer, solange ihr Simulationsstand derselbe ist.</summary>
        private VariantenDaten Daten(VarianteZeile v)
        {
            string stand = v.SimZeitpunkt ?? "";
            if (_puffer.TryGetValue(v.IdProjekt, out var p) && p.Stand == stand) return p.Daten;
            VariantenDaten d = stand.Length == 0 ? new VariantenDaten { IdProjekt = v.IdProjekt, IstStamm = v.IstStamm }
                                                 : _lade(v.IdProjekt, v.IstStamm);
            _puffer[v.IdProjekt] = (stand, d);
            return d;
        }

        /// <summary>Die Zeilen des Kerns als Gruppen der Razor-Komponente.</summary>
        internal static IReadOnlyList<ErgebnisVergleichGruppe> Abbilden(EnergievergleichErgebnis vergleich)
        {
            var gruppen = new List<ErgebnisVergleichGruppe>();
            foreach (string g in Energievergleich.GRUPPEN)
            {
                List<ErgebnisVergleichZeile> zeilen = vergleich.Zeilen.Where(z => z.Gruppe == g).Select(z =>
                    new ErgebnisVergleichZeile
                    {
                        Schluessel = z.Schluessel,
                        Bezeichnung = z.Einheit.Length == 0 ? z.Bezeichnung : z.Bezeichnung + " [" + z.Einheit + "]",
                        OhneWert = UebersichtErgebnisAnzeige.OhneWert(z.Werte),
                        Kennzahl = z.Bezeichnung,
                        Einheit = z.Einheit,
                        Zellen = z.Texte.Select((t, i) => new ErgebnisVergleichZelle
                        {
                            Text = t,
                            WeichtAb = z.WeichtAb[i],
                            Abweichung = z.Abweichung[i],
                            NichtSimuliert = !vergleich.Simuliert[i],
                            Wert = z.Werte[i]
                        }).ToList()
                    }).ToList();
                if (zeilen.Count > 0)
                    gruppen.Add(new ErgebnisVergleichGruppe
                    {
                        Schluessel = g,
                        Titel = Gruppentitel(g),
                        Zeilen = zeilen,
                        // Die Vorgabe des Kerns; den Benutzerzustand setzt UebersichtErgebnisAnzeige.Anwenden.
                        Zustand = Energievergleich.VorgabeOffen(g) ? ErgebnisGruppenZustand.Offen
                                                                   : ErgebnisGruppenZustand.Zugeklappt
                    });
            }
            return gruppen;
        }

        /// <summary>Der Anzeigetitel eines Gruppenschlüssels.</summary>
        internal static string Gruppentitel(string gruppe)
        {
            switch (gruppe)
            {
                case Energievergleich.GRUPPE_BEDARF: return MyResource.Resource.BK_ERG_GR_BEDARF;
                case Energievergleich.GRUPPE_ERZEUGUNG: return MyResource.Resource.BK_ERG_GR_ERZEUGUNG;
                case Energievergleich.GRUPPE_STROM: return MyResource.Resource.BK_ERG_GR_STROM;
                case Energievergleich.GRUPPE_ENDENERGIE: return MyResource.Resource.BK_ERG_GR_ENDENERGIE;
                case Energievergleich.GRUPPE_DECKUNG: return MyResource.Resource.BK_ERG_GR_DECKUNG;
                case Energievergleich.GRUPPE_KAELTE: return MyResource.Resource.BK_ERG_GR_KAELTE;
                case Energievergleich.GRUPPE_EFFIZIENZ: return MyResource.Resource.BK_ERG_GR_EFFIZIENZ;
                case Energievergleich.GRUPPE_EFFIZIENZ_WEITERE: return MyResource.Resource.BK_ERG_GR_EFFIZIENZ_WEITERE;
                case Energievergleich.GRUPPE_GEBAEUDE: return MyResource.Resource.BK_ERG_GR_GEBAEUDE;
                case Energievergleich.GRUPPE_EMISSIONEN: return MyResource.Resource.BK_ERG_GR_EMISSIONEN;
                default: return gruppe;
            }
        }
    }
}
