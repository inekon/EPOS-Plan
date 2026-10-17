using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Art eines Knotens im Energieflussbild (Konzept Energieflussbild, Abschnitte 2.2 und 3.1) — das eine Wort, das
    /// Bilanz (<see cref="EnergieflussBilanz"/>), Abbildung (<see cref="EnergieflussAbbildung"/>) und Renderer
    /// (<see cref="EnergieflussBildKnoten"/>) teilen. Sie bestimmt die Bilanzprüfung und im Bild die Lage der Beschriftung.
    /// </summary>
    public enum EnergieflussKnotenart
    {
        /// <summary>Spalte 0: Netzbezug, Photovoltaik, Brennstoffträger, Umweltanteile, Kondensationswärme, Solarthermie, ungedeckt; Beschriftung links.</summary>
        Quelle = 0,
        /// <summary>Spalten 1 und 3: BHKW, Heizkessel; Wärmepumpe, Heizstab, Elektrokessel, Kälteerzeuger; Beschriftung über dem Knoten.</summary>
        Wandler = 1,
        /// <summary>Spalten 2 und 4: die Sammelknoten Strom, Wärme, Kälte; Beschriftung über dem Knoten, auch wenn er flach ist.</summary>
        Bus = 2,
        /// <summary>Spalte 5: Puffer-, Kälte- und Stromspeicher; Beschriftung über dem Knoten.</summary>
        Speicher = 3,
        /// <summary>Spalte 6: Bedarfe, Einspeisungen, nicht zugeordnet; Beschriftung rechts.</summary>
        Senke = 4,
        /// <summary>
        /// Verlust bzw. Füllstandsänderung eines Wandlers oder Speichers. Trägt die Spalte seines Eigentümers (des Knotens,
        /// aus dem sein Band kommt); in die Lücke zur nächsten Spalte (Spalte + ½, unter dem Fuß des Eigentümers) setzt ihn
        /// allein der Renderer — die Lage ist Zeichenregel, kein Datum. Beschriftung darunter.
        /// </summary>
        Verlust = 5,
    }

    /// <summary>Die Energieform eines Bandes — fachlich, keine Farbe (die Farbrolle vergibt erst die Abbildung auf den Renderer).</summary>
    public enum EnergieflussForm
    {
        Strom,
        Waerme,
        Kaelte,
        Brennstoff,
        /// <summary>Umweltwärme, Umwelt (Kälte), Kondensationswärme.</summary>
        Umwelt,
        Verlust,
        /// <summary>„ungedeckt“ und „nicht zugeordnet“.</summary>
        Rest,
    }

    /// <summary>Woher die Zahl eines Bandes kommt (Konzept 1.6).</summary>
    public enum EnergieflussHerkunft
    {
        /// <summary>Ein Aggregat des gespeicherten Ergebnisbaums.</summary>
        Direkt,
        /// <summary>Aus Aggregaten gerechnet (Umweltanteil, Kondensationswärme, Verlust, Direktdeckung).</summary>
        Abgeleitet,
        /// <summary>Der Rest am Stromknoten (Einspeisung BHKW) bzw. ein nicht zugeordneter Bilanzrest.</summary>
        Bilanziert,
    }

    /// <summary>Ein Knoten des Energieflussbilds. Alle Mengen in MWh/a.</summary>
    public sealed class EnergieflussKnoten
    {
        /// <summary>Stabile Kennung („strom“, „traeger:12“, „puffer:3“, „verlust:bhkw“ …).</summary>
        public string Id = "";
        public string Name = "";
        /// <summary>Kurzform für die schmale Ansicht.</summary>
        public string Kurzname = "";
        public EnergieflussKnotenart Art;
        /// <summary>Spalte 0 … 6 (Tabelle 2.2); ein Verlust- oder Füllstandsknoten trägt die Spalte seines Eigentümers.</summary>
        public int Spalte;
        /// <summary>Reihenfolge von oben innerhalb der Spalte (Tabelle 2.2).</summary>
        public int Rang;
        public double ZuflussMwh;
        public double AbflussMwh;
        /// <summary>Zufluss − Abfluss VOR einem Ausgleichsband („Einspeisung BHKW“, „nicht zugeordnet“); 0 bei Quelle, Senke, Verlust.</summary>
        public double RestMwh;
        /// <summary>Wird die Bilanz dieses Knotens geprüft (Wandler, Bus, Speicher)?</summary>
        public bool Geprueft;
        /// <summary>Schließt der Knoten ohne „nicht zugeordnet“ (|Rest| ≤ Toleranz oder bilanzierte Einspeisung)?</summary>
        public bool Geschlossen = true;
        /// <summary>Zeigertext („Name: Zufluss x · Abfluss y MWh/a“, samt Zusätzen).</summary>
        public string Zeigertext = "";
        /// <summary>Die einzelnen Namen eines gebündelten Knotens („weitere (n)“); sonst leer.</summary>
        public List<string> Gebuendelt = new List<string>();
        /// <summary>Marke für den Zeiger des Renderers.</summary>
        public string Marke => "knoten:" + Id;

        // Bündelgruppe (Brennstoffträger, Split-Zeilen, Pufferspeicher) und Zusatz des Zeigertexts.
        internal string Gruppe;
        internal string Zusatz;
        internal bool IstBedarf;
    }

    /// <summary>Ein Band des Energieflussbilds: <see cref="Von"/> → <see cref="Nach"/> mit <see cref="Mwh"/> MWh/a.</summary>
    public sealed class EnergieflussFluss
    {
        public string Von = "";
        public string Nach = "";
        public double Mwh;
        public EnergieflussForm Form;
        public EnergieflussHerkunft Herkunft;
        /// <summary>Rechenweg eines abgeleiteten Bandes („Wärme − Strom“); sonst leer.</summary>
        public string Formel = "";
        /// <summary>Anteil in Prozent: am Zufluss des Ziels (Wandler, Bus, Speicher, Bedarf) bzw. am Zufluss der Quelle.</summary>
        public double AnteilProzent;
        /// <summary>„Von → Nach: x MWh/a (y %)“ samt „· bilanziert …“ bzw. „· abgeleitet: …“.</summary>
        public string Zeigertext = "";
        /// <summary>Marke für den Zeiger des Renderers.</summary>
        public string Marke => "fluss:" + Von + ":" + Nach;
    }

    /// <summary>
    /// <b>Die Energieflussbilanz einer Version</b> — das Domänenmodell des Energieflussbilds: Knoten, Bänder, Bilanz je
    /// Knoten (Zufluss, Abfluss, Rest, geschlossen), Hinweiszeilen und die Flussliste des Assistenten. Keine Farbe, kein
    /// Bildpunkt: Das Bild entsteht über <see cref="EnergieflussAbbildung.Bild"/> (→ <see cref="EnergieflussBildDaten"/>)
    /// und <c>ChartRenderer.EnergieflussModell</c> (→ <c>Zeichenmodell</c>); die Hinweise bleiben hier.
    /// </summary>
    public sealed class EnergieflussBilanz
    {
        public int IdProjekt;
        /// <summary>Anzeigename der Version.</summary>
        public string Version = "";
        public bool Englisch;
        /// <summary>
        /// Die Darstellung, für die die Bilanz gebaut ist: <see cref="EnergieflussDarstellung.Zuordnung"/> führt die Wärmeseite
        /// als Bänder Erzeuger → Verwendung, jede andere als Kette über den Wärmebus (Kette und Stufen teilen dieselbe Bilanz).
        /// </summary>
        public EnergieflussDarstellung Darstellung = EnergieflussDarstellung.Kette;
        /// <summary>Liegt ein gespeichertes Simulationsergebnis vor?</summary>
        public bool Simuliert;
        /// <summary>Benannter Grund, warum kein Bild entsteht (nicht simuliert, keine Flüsse); sonst leer.</summary>
        public string Grund = "";
        public List<EnergieflussKnoten> Knoten = new List<EnergieflussKnoten>();
        public List<EnergieflussFluss> Fluesse = new List<EnergieflussFluss>();
        /// <summary>Hinweiszeilen unter dem Bild (nicht geschlossene Knoten).</summary>
        public List<string> Hinweise = new List<string>();

        /// <summary>Kein Band — kein Bild.</summary>
        public bool Leer => Fluesse.Count == 0;

        public EnergieflussKnoten KnotenMit(string id) => Knoten.FirstOrDefault(k => k.Id == id);

        /// <summary>Die Menge des Bandes <paramref name="von"/> → <paramref name="nach"/> [MWh/a]; 0, wenn es fehlt.</summary>
        public double Fluss(string von, string nach)
            => Fluesse.Where(f => f.Von == von && f.Nach == nach).Sum(f => f.Mwh);

        public EnergieflussFluss FlussMit(string von, string nach)
            => Fluesse.FirstOrDefault(f => f.Von == von && f.Nach == nach);

        /// <summary>
        /// Die Flussliste für den Assistenten (Konzept 4.5): Kopfzeile, je Band „Von → Nach: x MWh/a (y %) · Herkunft“,
        /// danach die Hinweiszeilen; ohne Bild der Grund.
        /// </summary>
        public string KiText()
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, Energiefluss.T(nameof(MyResource.Resource.EF_KI_KOPF), Englisch), Version));
            if (Leer)
            {
                if (!string.IsNullOrEmpty(Grund)) sb.AppendLine(Grund);
                return sb.ToString();
            }
            CultureInfo k = Energiefluss.Kultur(Englisch);
            foreach (EnergieflussFluss f in Fluesse)
            {
                string von = KnotenMit(f.Von)?.Name ?? f.Von, nach = KnotenMit(f.Nach)?.Name ?? f.Nach;
                sb.Append(string.Format(k, Energiefluss.T(nameof(MyResource.Resource.EF_ZEIGER_FLUSS), Englisch),
                                        von, nach, Energiefluss.Zahl(f.Mwh, k), Energiefluss.Prozent(f.AnteilProzent, k)));
                sb.Append(" · ").AppendLine(Energiefluss.HerkunftText(f.Herkunft, Englisch));
            }
            foreach (string h in Hinweise) sb.AppendLine(h);
            return sb.ToString();
        }
    }

    /// <summary>
    /// <b>Der Flussbau des Energieflussbilds</b> (Konzept Energieflussbild, Abschnitte 1 und 2): aus dem gespeicherten
    /// Ergebnisbaum einer Version (<see cref="VariantenDaten"/>, dieselbe, die <see cref="EnergievergleichCtrl.Lade"/>
    /// baut) Knoten, Bänder und die Bilanz je Knoten — ohne Datenbank, ohne Lauf. Endenergie-Sicht mit drei Bussen
    /// (Strom, Wärme, Kälte); Umweltanteil als Zufluss (SK-Q6), Speicher als Zwischenknoten (SK-Q9), Verluste in der
    /// Lücke (SK-Q7), Kondensationswärme beim Brennwertkessel (SK-Q4), Solarüberschuss nicht gezeichnet (SK-Q3),
    /// BHKW-Einspeisung direkt aus der Ergebniszeile (EB-1, <see cref="ErgebnisBHKWModel.StromeinspeisungMwh"/>; ein
    /// Altergebnis ohne Wert wird am Stromknoten bilanziert, SK-Q1), Standby des Speichersystems als benanntes Verlustband
    /// am Strombus (EB-1), PV-Abregelung als grauer Abzweig „abgeregelt“ an der PV-Quelle (EB-1, EB-Q1: die Quelle trägt
    /// das erzeugbare Potenzial = abgegeben + abgeregelt; unter 1 % der Erzeugung nur im Zeigertext, SK-Q15), ein nicht
    /// geschlossener Knoten mit Band „nicht zugeordnet“ (SK-Q5).
    /// Alle Mengen in MWh/a; die kWh der Speicherzeilen rechnet allein <see cref="AusKwh"/> um.
    /// </summary>
    public static class Energiefluss
    {
        // ---- Spalten (Tabelle 2.2) ----
        public const int SPALTE_QUELLEN = 0, SPALTE_WANDLER_I = 1, SPALTE_STROM = 2, SPALTE_WANDLER_II = 3,
                         SPALTE_BUSSE = 4, SPALTE_SPEICHER = 5, SPALTE_SENKEN = 6;

        // ---- Schwellen ----
        /// <summary>Absolute Bilanztoleranz eines Knotens [MWh/a].</summary>
        public const double TOLERANZ_ABSOLUT_MWH = 0.05;
        /// <summary>Relative Bilanztoleranz eines Knotens (Anteil am Zufluss).</summary>
        public const double TOLERANZ_RELATIV = 0.005;
        /// <summary>Ein Band unter dieser Menge [MWh/a] erscheint nicht (unter der Anzeigerundung).</summary>
        public const double MINDEST_MWH = 0.0005;
        /// <summary>Gebündelt wird, was zusammen unter diesem Anteil des zugehörigen Busses liegt.</summary>
        public const double BUENDEL_ANTEIL = 0.01;
        /// <summary>Gebündelt wird erst, wenn es MEHR als so viele kleine Knoten gibt.</summary>
        public const int BUENDEL_AB_ANZAHL = 3;
        /// <summary>Ab diesem Anteil an der Ladung bekommt die Füllstandsänderung eines Speichers ein eigenes Band.</summary>
        public const double FUELLSTAND_EIGENES_BAND_ANTEIL = 0.01;

        // ---- Kennungen ----
        public const string NETZBEZUG = "netzbezug", PHOTOVOLTAIK = "photovoltaik", TRAEGER = "traeger:",
                            UMWELTWAERME = "umweltwaerme", UMWELTKAELTE = "umweltkaelte", KONDENSATION = "kondensation",
                            SOLARTHERMIE = "solarthermie", KUEHLSTROM_EIGEN = "kuehlstrom_eigen", UNGEDECKT = "ungedeckt",
                            NICHT_ZUGEORDNET_QUELLE = "nicht_zugeordnet_quelle",
                            BHKW = "bhkw", HEIZKESSEL = "heizkessel", STROM = "strom",
                            WP_HEIZEN = "wp_heizen", HEIZSTAB = "heizstab", ELEKTROKESSEL = "elektrokessel",
                            WP_KUEHLEN = "wp_kuehlen", KAELTEMASCHINE = "kaeltemaschine:", FREIE_KUEHLUNG = "freie_kuehlung",
                            SPLIT = "split:", WAERME = "waerme", KAELTE = "kaelte",
                            PUFFER = "puffer:", KAELTESPEICHER = "kaeltespeicher:", STROMSPEICHER = "stromspeicher:",
                            HEIZUNG = "heizung", WARMWASSER = "warmwasser", PROZESSWAERME = "prozesswaerme",
                            KUEHLUNG = "kuehlung", STROMBEDARF = "strombedarf", EINSPEISUNG_PV = "einspeisung_pv",
                            EINSPEISUNG_BHKW = "einspeisung_bhkw", NICHT_ZUGEORDNET = "nicht_zugeordnet",
                            VERLUST = "verlust:", FUELLSTAND = "fuellstand:", WEITERE = "weitere:",
                            STANDBY = "standby:", ABREGELUNG_PV = "abregelung_pv", SPEICHERVERLUST = "speicherverlust:";

        internal const string GRUPPE_TRAEGER = "traeger", GRUPPE_SPLIT = "split", GRUPPE_PUFFER = "puffer";
        private const string BRENNSTOFF_STROM = "Strom";

        /// <summary>Die Bilanztoleranz eines Knotens: max(0,05 MWh; 0,5 % des Zuflusses).</summary>
        public static double Toleranz(double zuflussMwh) => Math.Max(TOLERANZ_ABSOLUT_MWH, TOLERANZ_RELATIV * Math.Abs(zuflussMwh));

        /// <summary>Die EINE Umrechnung der Speicherzeilen (kWh/a) in die Einheit des Bildes (MWh/a).</summary>
        public static double AusKwh(double kWh) => Energieeinheit.MWh.AusKWh(kWh);

        /// <summary>Der Ressourcentext in der Sprache des Bildes (neutral = deutsch).</summary>
        internal static string T(string schluessel, bool englisch)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel, englisch ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.InvariantCulture); }
            catch { t = null; }
            return t ?? schluessel;
        }

        internal static CultureInfo Kultur(bool englisch) => CultureInfo.GetCultureInfo(englisch ? "en-US" : "de-DE");

        /// <summary>Zahl für Zeigertexte: unter 10 mit zwei, sonst mit einer Nachkommastelle.</summary>
        internal static string Zahl(double mwh, CultureInfo k) => mwh.ToString(Math.Abs(mwh) < 10.0 ? "N2" : "N1", k);

        internal static string Prozent(double p, CultureInfo k) => p.ToString(Math.Abs(p) < 1.0 ? "N1" : "N0", k);

        internal static string HerkunftText(EnergieflussHerkunft h, bool englisch)
            => T(h == EnergieflussHerkunft.Direkt ? nameof(MyResource.Resource.EF_HERKUNFT_DIREKT)
                 : h == EnergieflussHerkunft.Abgeleitet ? nameof(MyResource.Resource.EF_HERKUNFT_ABGELEITET)
                 : nameof(MyResource.Resource.EF_HERKUNFT_BILANZIERT), englisch);

        /// <summary>Der Name der Version für Kopf und Grund: Variante, sonst Projekt, sonst die Kennung.</summary>
        public static string Versionsname(VariantenDaten v)
        {
            if (v == null) return "";
            if (!string.IsNullOrWhiteSpace(v.Variantenname)) return v.Variantenname.Trim();
            if (!string.IsNullOrWhiteSpace(v.Projektname)) return v.Projektname.Trim();
            return v.IdProjekt.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die Energieflussbilanz einer Version aus den Daten des Energievergleichs; <paramref name="darstellung"/> =
        /// <see cref="EnergieflussDarstellung.Zuordnung"/> baut die Wärmeseite als Zuordnung Erzeuger → Verwendung.
        /// </summary>
        public static EnergieflussBilanz Aus(VariantenDaten v, bool englisch, string version = null,
                                             EnergieflussDarstellung darstellung = EnergieflussDarstellung.Kette)
        {
            string name = version ?? Versionsname(v);
            return Aus(v?.Ergebnis, v?.EnergiekostenJeTraeger, englisch, name, v?.IdProjekt ?? 0, darstellung);
        }

        /// <summary>
        /// Die Energieflussbilanz aus einem Ergebnisbaum; <paramref name="traeger"/> liefert die Namen der Brennstoffträger
        /// (<see cref="VariantenDaten.EnergiekostenJeTraeger"/>), ohne sie gilt das Brennstoffwort des Moduls. Mit
        /// <paramref name="darstellung"/> = <see cref="EnergieflussDarstellung.Zuordnung"/> entsteht die Wärmeseite als
        /// Zuordnung (siehe <c>Bau.WaermeZuordnen</c>); Kette und Stufen teilen die Bilanz der Kette.
        /// </summary>
        public static EnergieflussBilanz Aus(ErgebnisModel e, IReadOnlyList<EnergieTraegerNachweis> traeger, bool englisch,
                                             string version, int idProjekt = 0,
                                             EnergieflussDarstellung darstellung = EnergieflussDarstellung.Kette)
        {
            var m = new EnergieflussBilanz
            {
                IdProjekt = idProjekt, Version = version ?? "", Englisch = englisch, Simuliert = e != null,
                Darstellung = darstellung == EnergieflussDarstellung.Zuordnung ? EnergieflussDarstellung.Zuordnung : EnergieflussDarstellung.Kette
            };
            if (e == null)
            {
                m.Grund = string.Format(Kultur(englisch), T(nameof(MyResource.Resource.EF_NICHT_SIMULIERT), englisch), m.Version);
                return m;
            }
            new Bau(m, e, traeger, englisch).Fuehren();
            if (m.Leer)
                m.Grund = string.Format(Kultur(englisch), T(nameof(MyResource.Resource.EF_LEER), englisch), m.Version);
            return m;
        }

        // =====================================================================
        //  Der Bau
        // =====================================================================

        private sealed class Bau
        {
            private readonly EnergieflussBilanz _m;
            private readonly ErgebnisModel _e;
            private readonly IReadOnlyList<EnergieTraegerNachweis> _traeger;
            private readonly bool _en;
            private readonly CultureInfo _k;
            private readonly Dictionary<string, EnergieflussKnoten> _knoten = new Dictionary<string, EnergieflussKnoten>();
            private readonly List<EnergieflussFluss> _fluesse = new List<EnergieflussFluss>();
            private int _folge;
            private double _bhkwStromMwh;

            /// <summary>
            /// Quellentnahme der Wärmepumpe je Wärmespeicher [MWh] (Booster-Kette): Wärme, die
            /// eine Wärmepumpe erzeugt, in einen Puffer geladen und eine zweite als Quellwärme
            /// wieder entnommen hat. Sie läuft im Kreis und wird deshalb gegen die Wärme der
            /// Wärmepumpe und die Ladung des Puffers aufgerechnet, statt den Wärmebus doppelt
            /// zu füllen.
            /// </summary>
            private readonly Dictionary<ErgebnisPufferspeicherModel, double> _quellentnahme =
                new Dictionary<ErgebnisPufferspeicherModel, double>();
            /// <summary>EB-1: die Einspeisung der BHKW-Ergebniszeile; <c>null</c> = nicht erhoben (dann bilanziert, SK-Q1).</summary>
            private double? _bhkwEinspeisungMwh;
            /// <summary>Die Wärmeseite als Zuordnung Erzeuger → Verwendung (<see cref="EnergieflussDarstellung.Zuordnung"/>)?</summary>
            private readonly bool _zuordnung;
            /// <summary>Der Zufluss des Wärmebusses vor der Zuordnung — Bezug der Bündelung, die die Kette am Bus misst.</summary>
            private double _waermeBezugMwh = double.NaN;

            public Bau(EnergieflussBilanz m, ErgebnisModel e, IReadOnlyList<EnergieTraegerNachweis> traeger, bool englisch)
            {
                _m = m; _e = e; _traeger = traeger; _en = englisch; _k = Kultur(englisch);
                _zuordnung = m.Darstellung == EnergieflussDarstellung.Zuordnung;
            }

            private string T(string s) => Energiefluss.T(s, _en);

            // Rang: Grundrang der Art (Reihenfolge der Tabelle 2.2) und Einfügefolge.
            private EnergieflussKnoten K(string id, string name, string kurz, EnergieflussKnotenart art, int spalte, int grundrang,
                                         string gruppe = null, bool bedarf = false)
            {
                if (_knoten.TryGetValue(id, out EnergieflussKnoten k)) return k;
                k = new EnergieflussKnoten
                {
                    Id = id, Name = name, Kurzname = kurz, Art = art, Spalte = spalte, Rang = grundrang * 1000 + (_folge++),
                    Geprueft = art == EnergieflussKnotenart.Wandler || art == EnergieflussKnotenart.Bus || art == EnergieflussKnotenart.Speicher,
                    Gruppe = gruppe, IstBedarf = bedarf,
                };
                _knoten[id] = k;
                return k;
            }

            private EnergieflussKnoten KR(string id, string schluesselName, string schluesselKurz, EnergieflussKnotenart art, int spalte,
                                          int grundrang, bool bedarf = false)
                => K(id, T(schluesselName), T(schluesselKurz), art, spalte, grundrang, null, bedarf);

            private void F(string von, string nach, double mwh, EnergieflussForm form,
                           EnergieflussHerkunft herkunft = EnergieflussHerkunft.Direkt, string formel = "")
            {
                if (!(mwh >= MINDEST_MWH) || double.IsInfinity(mwh)) return;
                _fluesse.Add(new EnergieflussFluss { Von = von, Nach = nach, Mwh = mwh, Form = form, Herkunft = herkunft, Formel = formel ?? "" });
            }

            private double Zu(string id) => _fluesse.Where(f => f.Nach == id).Sum(f => f.Mwh);
            private double Ab(string id) => _fluesse.Where(f => f.Von == id).Sum(f => f.Mwh);
            private bool Hat(string id) => _fluesse.Any(f => f.Von == id || f.Nach == id);

            // Grundränge in der Reihenfolge der Tabelle 2.2.
            private const int R_NETZ = 1, R_PV = 2, R_TRAEGER = 3, R_UMWELT_W = 4, R_UMWELT_K = 5, R_KOND = 6, R_SOLAR = 7,
                              R_KSTROM = 8, R_UNGEDECKT = 9, R_NZ_QUELLE = 10,
                              R_BHKW = 11, R_KESSEL = 12, R_STROM = 13,
                              R_WP = 14, R_HEIZSTAB = 15, R_EKESSEL = 16, R_WPK = 17, R_KM = 18, R_FREI = 19, R_SPLIT = 20,
                              R_WAERME = 21, R_KAELTE = 22, R_PUFFER = 23, R_KSPEICHER = 24, R_SSPEICHER = 25,
                              R_HEIZUNG = 26, R_WW = 27, R_PROZESS = 28, R_KUEHLUNG = 29, R_STROMBEDARF = 30,
                              R_EINSP_PV = 31, R_EINSP_BHKW = 32, R_NZ = 33;

            public void Fuehren()
            {
                ErgebnisEnergiebedarfModel eb = _e.Energiebedarf;
                bool kuehlenWp = (_e.Waermepumpe?.Kaelteproduktion_WP ?? 0.0) >= MINDEST_MWH;
                bool heizenWp = _e.Waermepumpe != null && _e.Waermepumpe.Waermeproduktion_WP >= MINDEST_MWH;

                // ---- Busse und Senken (nur angelegt; erscheinen, wenn ein Band an ihnen hängt) ----
                KR(STROM, nameof(MyResource.Resource.EF_KN_STROM), nameof(MyResource.Resource.EF_KK_STROM), EnergieflussKnotenart.Bus, SPALTE_STROM, R_STROM);
                KR(WAERME, nameof(MyResource.Resource.EF_KN_WAERME), nameof(MyResource.Resource.EF_KK_WAERME), EnergieflussKnotenart.Bus, SPALTE_BUSSE, R_WAERME);
                KR(KAELTE, nameof(MyResource.Resource.EF_KN_KAELTE), nameof(MyResource.Resource.EF_KK_KAELTE), EnergieflussKnotenart.Bus, SPALTE_BUSSE, R_KAELTE);

                // ---- Quellen am Strom ----
                if (eb != null)
                {
                    KR(NETZBEZUG, nameof(MyResource.Resource.EF_KN_NETZBEZUG), nameof(MyResource.Resource.EF_KK_NETZBEZUG), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_NETZ);
                    F(NETZBEZUG, STROM, eb.Stromrestbedarf, EnergieflussForm.Strom);
                }
                if (_e.Photovoltaik != null)
                {
                    KR(PHOTOVOLTAIK, nameof(MyResource.Resource.EF_KN_PHOTOVOLTAIK), nameof(MyResource.Resource.EF_KK_PHOTOVOLTAIK), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_PV);
                    // EB-1 (EB-Q1): Die Erzeugung der Module ist das erzeugbare Potenzial; die an der Einspeisegrenze
                    // abgeregelte Menge verlässt die Quelle als grauer Abzweig „abgeregelt“, an den Strombus geht der Rest.
                    // Ein Altergebnis ohne Wert (null) und ein Lauf ohne Grenze (0) zeichnen wie bisher.
                    double abregelung = Math.Max(0.0, Math.Min(_e.Photovoltaik.AbregelungMwh ?? 0.0, _e.Photovoltaik.Stromproduktion));
                    if (abregelung >= MINDEST_MWH)
                    {
                        KR(ABREGELUNG_PV, nameof(MyResource.Resource.EF_KN_ABREGELUNG), nameof(MyResource.Resource.EF_KK_ABREGELUNG), EnergieflussKnotenart.Verlust, SPALTE_QUELLEN, R_PV);
                        F(PHOTOVOLTAIK, ABREGELUNG_PV, abregelung, EnergieflussForm.Verlust);
                    }
                    F(PHOTOVOLTAIK, STROM, _e.Photovoltaik.Stromproduktion - abregelung, EnergieflussForm.Strom);
                }

                // ---- Wandler I: BHKW ----
                if (_e.BHKW != null)
                {
                    KR(BHKW, nameof(MyResource.Resource.EF_KN_BHKW), nameof(MyResource.Resource.EF_KK_BHKW), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_I, R_BHKW);
                    foreach (ErgebnisBHKWModulModel mo in _e.BHKW.Module ?? new List<ErgebnisBHKWModulModel>())
                        if (mo != null && mo.Verbrauch >= MINDEST_MWH)
                            F(Traeger(mo.CarrierId, mo.Brennstoff), BHKW, mo.Verbrauch, EnergieflussForm.Brennstoff);
                    _bhkwStromMwh = _e.BHKW.Stromproduktion;
                    _bhkwEinspeisungMwh = _e.BHKW.StromeinspeisungMwh;
                    F(BHKW, STROM, _e.BHKW.Stromproduktion, EnergieflussForm.Strom);
                    F(BHKW, WAERME, _e.BHKW.Waermeproduktion, EnergieflussForm.Waerme);
                    VerlustWandler(BHKW, SPALTE_WANDLER_I, R_BHKW);
                }

                // ---- Wandler I: Heizkessel (Brennstoff); Wandler II: Elektrokessel ----
                if (_e.Heizkessel != null) Kessel(_e.Heizkessel);

                // ---- Wandler II: Wärmepumpe (Heizen), Heizstab ----
                if (_e.Waermepumpe != null)
                {
                    ErgebnisWaermepumpeModel w = _e.Waermepumpe;
                    if (heizenWp)
                    {
                        if (kuehlenWp)
                            KR(WP_HEIZEN, nameof(MyResource.Resource.EF_KN_WP_HEIZEN), nameof(MyResource.Resource.EF_KK_WP_HEIZEN), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_WP);
                        else
                            KR(WP_HEIZEN, nameof(MyResource.Resource.EF_KN_WAERMEPUMPE), nameof(MyResource.Resource.EF_KK_WAERMEPUMPE), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_WP);
                        F(STROM, WP_HEIZEN, w.Stromverbrauch_WP, EnergieflussForm.Strom);
                        F(WP_HEIZEN, WAERME, w.Waermeproduktion_WP - QuellentnahmenAufrechnen(w.Waermeproduktion_WP), EnergieflussForm.Waerme);
                        Umweltanteil(WP_HEIZEN, UMWELTWAERME, SPALTE_WANDLER_II, R_WP, waerme: true);
                    }
                    if (w.Stromverbrauch_Heizstab >= MINDEST_MWH)
                    {
                        KR(HEIZSTAB, nameof(MyResource.Resource.EF_KN_HEIZSTAB), nameof(MyResource.Resource.EF_KK_HEIZSTAB), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_HEIZSTAB);
                        F(STROM, HEIZSTAB, w.Stromverbrauch_Heizstab, EnergieflussForm.Strom);
                        F(HEIZSTAB, WAERME, w.Stromverbrauch_Heizstab, EnergieflussForm.Waerme);
                    }
                }

                // ---- Solarthermie (Quelle direkt an den Wärmebus) ----
                if (_e.Solarthermie != null)
                {
                    EnergieflussKnoten s = KR(SOLARTHERMIE, nameof(MyResource.Resource.EF_KN_SOLARTHERMIE), nameof(MyResource.Resource.EF_KK_SOLARTHERMIE), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_SOLAR);
                    F(SOLARTHERMIE, WAERME, _e.Solarthermie.Waermeproduktion, EnergieflussForm.Waerme);
                    if (_e.Solarthermie.Ueberschuss >= MINDEST_MWH)
                        s.Zusatz = string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_SOLARUEBERSCHUSS)), Zahl(_e.Solarthermie.Ueberschuss, _k));
                }

                // ---- Kälteerzeuger ----
                Kaelteerzeuger(kuehlenWp);

                // ---- Speicher und Senken ----
                Waermeseite(eb);
                Kaelteseite(eb);
                Stromseite(eb);
                if (_zuordnung) WaermeZuordnen(eb);

                // ---- Bilanz der Busse (die Wandler und Speicher schließen schon) ----
                BilanzStrom();
                Bilanz(WAERME);
                Bilanz(KAELTE);
                foreach (EnergieflussKnoten k in _knoten.Values.Where(k => k.Geprueft && k.Art != EnergieflussKnotenart.Bus).ToList())
                    Bilanz(k.Id);

                Buendeln();
                Abschliessen();
            }

            // ---------------------------------------------------------------
            //  Brennstoffträger
            // ---------------------------------------------------------------

            private string Traeger(int carrierId, string brennstoff)
            {
                string id, name;
                if (carrierId > 0)
                {
                    id = TRAEGER + carrierId.ToString(CultureInfo.InvariantCulture);
                    EnergieTraegerNachweis t = _traeger?.FirstOrDefault(x => x != null && x.CarrierId == carrierId && !string.IsNullOrWhiteSpace(x.Traeger));
                    name = t != null ? t.Traeger.Trim() : (!string.IsNullOrWhiteSpace(brennstoff) ? brennstoff.Trim() : T(nameof(MyResource.Resource.EF_KN_BRENNSTOFF)));
                }
                else
                {
                    string wort = string.IsNullOrWhiteSpace(brennstoff) ? "" : brennstoff.Trim();
                    id = TRAEGER + (wort.Length == 0 ? "0" : wort.ToLowerInvariant());
                    name = wort.Length == 0 ? T(nameof(MyResource.Resource.EF_KN_BRENNSTOFF)) : wort;
                }
                K(id, name, name, EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_TRAEGER, GRUPPE_TRAEGER);
                return id;
            }

            // ---------------------------------------------------------------
            //  Wandler: Verlust, Umweltanteil
            // ---------------------------------------------------------------

            /// <summary>Verlust eines Wandlers = Zufluss − Abfluss, nie negativ (ein Defizit bleibt Bilanzrest).</summary>
            private void VerlustWandler(string id, int spalte, int rang)
            {
                double v = Zu(id) - Ab(id);
                if (v < MINDEST_MWH) return;
                string vid = VERLUST + id;
                KR(vid, nameof(MyResource.Resource.EF_KN_VERLUST), nameof(MyResource.Resource.EF_KK_VERLUST), EnergieflussKnotenart.Verlust, spalte, rang);
                F(id, vid, v, EnergieflussForm.Verlust, EnergieflussHerkunft.Abgeleitet, T(nameof(MyResource.Resource.EF_FORMEL_VERLUST)));
            }

            /// <summary>
            /// Umweltanteil (SK-Q6): Abfluss − Strom tritt als Umweltwärme bzw. Umwelt (Kälte) ein; ein Wandler mit
            /// Arbeitszahl unter 1 bekommt statt dessen ein Verlustband.
            /// </summary>
            private void Umweltanteil(string id, string quelle, int spalte, int rang, bool waerme)
            {
                double u = Ab(id) - Zu(id);
                if (u >= MINDEST_MWH)
                {
                    EnergieflussKnoten q = waerme
                        ? KR(UMWELTWAERME, nameof(MyResource.Resource.EF_KN_UMWELTWAERME), nameof(MyResource.Resource.EF_KK_UMWELTWAERME), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_UMWELT_W)
                        : KR(UMWELTKAELTE, nameof(MyResource.Resource.EF_KN_UMWELTKAELTE), nameof(MyResource.Resource.EF_KK_UMWELTKAELTE), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_UMWELT_K);
                    q.Zusatz = T(nameof(MyResource.Resource.EF_ZEIGER_UMWELT));
                    F(quelle, id, u, EnergieflussForm.Umwelt, EnergieflussHerkunft.Abgeleitet,
                      T(waerme ? nameof(MyResource.Resource.EF_FORMEL_UMWELT_WAERME) : nameof(MyResource.Resource.EF_FORMEL_UMWELT_KAELTE)));
                }
                else VerlustWandler(id, spalte, rang);
            }

            // ---------------------------------------------------------------
            //  Kessel
            // ---------------------------------------------------------------

            private void Kessel(ErgebnisHeizkesselModel h)
            {
                double waermeBrennstoff = 0.0, waermeStrom = 0.0, kond = 0.0, verlust = 0.0;
                bool brennstoffKessel = false;
                List<ErgebnisHeizkesselModulModel> module = (h.Module ?? new List<ErgebnisHeizkesselModulModel>()).Where(x => x != null).ToList();
                if (module.Count == 0)
                {
                    waermeBrennstoff = h.Waermeproduktion;
                    brennstoffKessel = true;
                }
                foreach (ErgebnisHeizkesselModulModel mo in module)
                {
                    if (string.Equals((mo.Brennstoff ?? "").Trim(), BRENNSTOFF_STROM, StringComparison.OrdinalIgnoreCase))
                    {
                        waermeStrom += mo.Waermeproduktion;
                        continue;
                    }
                    brennstoffKessel = true;
                    waermeBrennstoff += mo.Waermeproduktion;
                    if (mo.Verbrauch >= MINDEST_MWH)
                    {
                        KR(HEIZKESSEL, nameof(MyResource.Resource.EF_KN_HEIZKESSEL), nameof(MyResource.Resource.EF_KK_HEIZKESSEL), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_I, R_KESSEL);
                        F(Traeger(mo.CarrierId, mo.Brennstoff), HEIZKESSEL, mo.Verbrauch, EnergieflussForm.Brennstoff);
                        // SK-Q4: Brennwert über 100 % (Heizwert) — die Mehrwärme tritt als Kondensationswärme ein.
                        kond += Math.Max(0.0, mo.Waermeproduktion - mo.Verbrauch);
                        verlust += Math.Max(0.0, mo.Verbrauch - mo.Waermeproduktion);
                    }
                }
                if (brennstoffKessel && waermeBrennstoff >= MINDEST_MWH)
                {
                    KR(HEIZKESSEL, nameof(MyResource.Resource.EF_KN_HEIZKESSEL), nameof(MyResource.Resource.EF_KK_HEIZKESSEL), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_I, R_KESSEL);
                    F(HEIZKESSEL, WAERME, waermeBrennstoff, EnergieflussForm.Waerme);
                    if (kond >= MINDEST_MWH)
                    {
                        KR(KONDENSATION, nameof(MyResource.Resource.EF_KN_KONDENSATION), nameof(MyResource.Resource.EF_KK_KONDENSATION), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_KOND);
                        F(KONDENSATION, HEIZKESSEL, kond, EnergieflussForm.Umwelt, EnergieflussHerkunft.Abgeleitet, T(nameof(MyResource.Resource.EF_FORMEL_KONDENSATION)));
                    }
                    if (verlust >= MINDEST_MWH)
                    {
                        string vid = VERLUST + HEIZKESSEL;
                        KR(vid, nameof(MyResource.Resource.EF_KN_VERLUST), nameof(MyResource.Resource.EF_KK_VERLUST), EnergieflussKnotenart.Verlust, SPALTE_WANDLER_I, R_KESSEL);
                        F(HEIZKESSEL, vid, verlust, EnergieflussForm.Verlust, EnergieflussHerkunft.Abgeleitet, T(nameof(MyResource.Resource.EF_FORMEL_VERLUST)));
                    }
                }
                if (waermeStrom >= MINDEST_MWH)
                {
                    KR(ELEKTROKESSEL, nameof(MyResource.Resource.EF_KN_ELEKTROKESSEL), nameof(MyResource.Resource.EF_KK_ELEKTROKESSEL), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_EKESSEL);
                    F(STROM, ELEKTROKESSEL, h.Stromverbrauch, EnergieflussForm.Strom);
                    F(ELEKTROKESSEL, WAERME, waermeStrom, EnergieflussForm.Waerme);
                    VerlustWandler(ELEKTROKESSEL, SPALTE_WANDLER_II, R_EKESSEL);
                }
            }

            // ---------------------------------------------------------------
            //  Kälteerzeuger: Wärmepumpe (Kühlen), Kältemaschinen, freie Kühlung, Split-Geräte
            // ---------------------------------------------------------------

            /// <summary>Eigener Zähler wirkt nur mit abweichendem Kühlträger (wie <c>Kaeltekaskade.NebenDerStufenrechnung</c>).</summary>
            private static bool EigenerZaehler(int? kuehlCarrierId, bool? eigenerZaehler)
                => (kuehlCarrierId ?? 0) > 0 && eigenerZaehler == true;

            private string StromquelleKaelte(bool? eigenerZaehler, int? kuehlCarrierId)
            {
                if (!EigenerZaehler(kuehlCarrierId, eigenerZaehler)) return STROM;
                KR(KUEHLSTROM_EIGEN, nameof(MyResource.Resource.EF_KN_KUEHLSTROM_EIGEN), nameof(MyResource.Resource.EF_KK_KUEHLSTROM_EIGEN), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_KSTROM);
                return KUEHLSTROM_EIGEN;
            }

            private void Kaelteerzeuger(bool kuehlenWp)
            {
                double frei = 0.0;
                if (kuehlenWp)
                {
                    ErgebnisWaermepumpeModel w = _e.Waermepumpe;
                    double freiWp = Math.Max(0.0, w.FreieKuehlung_MWh ?? 0.0);
                    frei += freiWp;
                    KR(WP_KUEHLEN, nameof(MyResource.Resource.EF_KN_WP_KUEHLEN), nameof(MyResource.Resource.EF_KK_WP_KUEHLEN), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_WPK);
                    // Kältestrom eines Moduls mit eigenem Zähler läuft neben der Stufenrechnung (nicht im Netzbezug).
                    double eigen = (w.Module ?? new List<ErgebnisWaermepumpeModulModel>())
                        .Where(mo => mo != null && EigenerZaehler(mo.Kuehl_CarrierId, mo.Kuehl_EigenerZaehler))
                        .Sum(mo => Math.Max(0.0, mo.Stromverbrauch_Kuehlung ?? 0.0));
                    double strom = w.Stromverbrauch_Kuehlung ?? 0.0;
                    eigen = Math.Min(eigen, Math.Max(0.0, strom));
                    if (eigen > 0.0) F(StromquelleKaelte(true, 1), WP_KUEHLEN, eigen, EnergieflussForm.Strom);
                    F(STROM, WP_KUEHLEN, strom - eigen, EnergieflussForm.Strom);
                    F(WP_KUEHLEN, KAELTE, (w.Kaelteproduktion_WP ?? 0.0) - freiWp, EnergieflussForm.Kaelte);
                    Umweltanteil(WP_KUEHLEN, UMWELTKAELTE, SPALTE_WANDLER_II, R_WPK, waerme: false);
                }
                int i = 0;
                foreach (ErgebnisKaeltemaschineModel km in _e.Kaeltemaschinen ?? new List<ErgebnisKaeltemaschineModel>())
                {
                    i++;
                    if (km == null) continue;
                    string id = KAELTEMASCHINE + i.ToString(CultureInfo.InvariantCulture);
                    string name = string.IsNullOrWhiteSpace(km.Bezeichner) ? T(nameof(MyResource.Resource.EF_KN_KAELTEMASCHINE)) : km.Bezeichner.Trim();
                    K(id, name, T(nameof(MyResource.Resource.EF_KK_KAELTEMASCHINE)), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_KM);
                    double freiKm = Math.Max(0.0, km.FreieKuehlung_MWh);
                    frei += freiKm;
                    F(StromquelleKaelte(km.Kuehl_EigenerZaehler, km.Kuehl_CarrierId), id, km.Stromverbrauch_MWh + km.Hilfsstrom_MWh, EnergieflussForm.Strom);
                    F(id, KAELTE, km.Kaelteproduktion_MWh - freiKm, EnergieflussForm.Kaelte);
                    if (Hat(id)) Umweltanteil(id, UMWELTKAELTE, SPALTE_WANDLER_II, R_KM, waerme: false);
                }
                if (frei >= MINDEST_MWH)
                {
                    KR(FREIE_KUEHLUNG, nameof(MyResource.Resource.EF_KN_FREIE_KUEHLUNG), nameof(MyResource.Resource.EF_KK_FREIE_KUEHLUNG), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_FREI);
                    F(FREIE_KUEHLUNG, KAELTE, frei, EnergieflussForm.Kaelte);
                    Umweltanteil(FREIE_KUEHLUNG, UMWELTKAELTE, SPALTE_WANDLER_II, R_FREI, waerme: false);
                }
                i = 0;
                foreach (ErgebnisKaelteDezentralModel s in _e.KaelteDezentral ?? new List<ErgebnisKaelteDezentralModel>())
                {
                    i++;
                    if (s == null) continue;
                    string id = SPLIT + i.ToString(CultureInfo.InvariantCulture);
                    string name = string.IsNullOrWhiteSpace(s.Bezeichner) ? T(nameof(MyResource.Resource.EF_KN_SPLIT)) : s.Bezeichner.Trim();
                    K(id, name, T(nameof(MyResource.Resource.EF_KK_SPLIT)), EnergieflussKnotenart.Wandler, SPALTE_WANDLER_II, R_SPLIT, GRUPPE_SPLIT);
                    F(StromquelleKaelte(s.Kuehl_EigenerZaehler, s.Kuehl_CarrierId), id, s.Strom_MWh, EnergieflussForm.Strom);
                    F(id, KAELTE, s.Kaelte_MWh, EnergieflussForm.Kaelte);
                    if (Hat(id)) Umweltanteil(id, UMWELTKAELTE, SPALTE_WANDLER_II, R_SPLIT, waerme: false);
                }
            }

            // ---------------------------------------------------------------
            //  Wärmeseite: Pufferspeicher, Senken je Kanal, ungedeckt
            // ---------------------------------------------------------------

            private static bool IstKaeltespeicher(ErgebnisPufferspeicherModel p)
                => string.Equals((p.Verwendung ?? "").Trim(), DbWerte.PSP_VERWENDUNG_KAELTE, StringComparison.OrdinalIgnoreCase);

            private static bool IstQuellspeicher(ErgebnisPufferspeicherModel p)
                => string.Equals((p.Verwendung ?? "").Trim(), DbWerte.PSP_VERWENDUNG_QUELLE, StringComparison.OrdinalIgnoreCase);

            /// <summary>
            /// Ein Speicher als Zwischenknoten (SK-Q9): Bus → Speicher (Ladung), Speicher → Senken (Entladung), Verlustband
            /// mit der Füllstandsänderung; ein eigenes Füllstandsband erst ab 1 % der Ladung.
            /// </summary>
            private void Speicher(string id, string bus, double ladung, double verlust, double fuellstand, EnergieflussForm form, int rang)
            {
                F(bus, id, ladung, form);
                verlust = Math.Max(0.0, verlust);
                fuellstand = Math.Max(0.0, fuellstand);
                bool eigenesBand = fuellstand >= FUELLSTAND_EIGENES_BAND_ANTEIL * ladung && fuellstand >= MINDEST_MWH;
                string vid = VERLUST + id;
                KR(vid, nameof(MyResource.Resource.EF_KN_VERLUST), nameof(MyResource.Resource.EF_KK_VERLUST), EnergieflussKnotenart.Verlust, SPALTE_SPEICHER, rang);
                if (eigenesBand)
                {
                    F(id, vid, verlust, EnergieflussForm.Verlust);
                    string fid = FUELLSTAND + id;
                    KR(fid, nameof(MyResource.Resource.EF_KN_FUELLSTAND), nameof(MyResource.Resource.EF_KK_FUELLSTAND), EnergieflussKnotenart.Verlust, SPALTE_SPEICHER, rang);
                    F(id, fid, fuellstand, form, EnergieflussHerkunft.Abgeleitet, T(nameof(MyResource.Resource.EF_FORMEL_FUELLSTAND)));
                }
                else F(id, vid, verlust + fuellstand, EnergieflussForm.Verlust);
                _knoten[id].Zusatz = string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_SPEICHER_VERLUST)), Zahl(verlust, _k), Zahl(fuellstand, _k));
            }

            private (string id, string name, string kurz, int rang) Senke(int kanal)
            {
                switch (kanal)
                {
                    case Kanal.HEIZUNG: return (HEIZUNG, nameof(MyResource.Resource.EF_KN_HEIZUNG), nameof(MyResource.Resource.EF_KK_HEIZUNG), R_HEIZUNG);
                    case Kanal.BRAUCHWASSER: return (WARMWASSER, nameof(MyResource.Resource.EF_KN_WARMWASSER), nameof(MyResource.Resource.EF_KK_WARMWASSER), R_WW);
                    case Kanal.PROZESS: return (PROZESSWAERME, nameof(MyResource.Resource.EF_KN_PROZESSWAERME), nameof(MyResource.Resource.EF_KK_PROZESSWAERME), R_PROZESS);
                    default: return (KUEHLUNG, nameof(MyResource.Resource.EF_KN_KUEHLUNG), nameof(MyResource.Resource.EF_KK_KUEHLUNG), R_KUEHLUNG);
                }
            }

            private void Waermeseite(ErgebnisEnergiebedarfModel eb)
            {
                var entladung = new double[Kanal.ANZAHL];
                foreach (ErgebnisPufferspeicherModel p in _e.Pufferspeicher ?? new List<ErgebnisPufferspeicherModel>())
                {
                    if (p == null || IstKaeltespeicher(p) || IstQuellspeicher(p)) continue;
                    double ladung = AusKwh(p.Ladung_gesamt);
                    if (ladung < MINDEST_MWH) continue;
                    string id = PUFFER + p.ID_Pufferspeicher.ToString(CultureInfo.InvariantCulture) + ":" + p.ID_Anlage.ToString(CultureInfo.InvariantCulture);
                    string name = string.IsNullOrWhiteSpace(p.Bezeichner) ? T(nameof(MyResource.Resource.EF_KN_PUFFERSPEICHER)) : p.Bezeichner.Trim();
                    K(id, name, T(nameof(MyResource.Resource.EF_KK_PUFFERSPEICHER)), EnergieflussKnotenart.Speicher, SPALTE_SPEICHER, R_PUFFER, GRUPPE_PUFFER);
                    foreach (int k in Kanal.KANAELE_WAERME)
                    {
                        // Was der Speicher über die Kanalzeilen hinaus abgibt, ist Quellentnahme
                        // einer Wärmepumpe und deckt keinen Bedarf (siehe QuellentnahmenAufrechnen).
                        double e = AusKwh(Math.Max(0.0, p.Entladung_Kanal?[k] ?? 0.0));
                        if (e < MINDEST_MWH) continue;
                        var s = Senke(k);
                        KR(s.id, s.name, s.kurz, EnergieflussKnotenart.Senke, SPALTE_SENKEN, s.rang, bedarf: true);
                        F(id, s.id, e, EnergieflussForm.Waerme);
                        entladung[k] += e;
                    }
                    _quellentnahme.TryGetValue(p, out double quell);
                    Speicher(id, WAERME, ladung - quell, AusKwh(p.Verluste_gesamt), AusKwh(p.SOC_Ende), EnergieflussForm.Waerme, R_PUFFER);
                }
                if (eb == null) return;
                double bedarf = 0.0;
                foreach (int k in Kanal.KANAELE_WAERME) bedarf += Math.Max(0.0, eb.Waermebedarf_Kanal?[k] ?? 0.0);
                double rest = Math.Max(0.0, eb.Waermerestbedarf);
                foreach (int k in Kanal.KANAELE_WAERME)
                {
                    double b = Math.Max(0.0, eb.Waermebedarf_Kanal?[k] ?? 0.0);
                    if (b < MINDEST_MWH) continue;
                    var s = Senke(k);
                    KR(s.id, s.name, s.kurz, EnergieflussKnotenart.Senke, SPALTE_SENKEN, s.rang, bedarf: true);
                    double ungedeckt = bedarf > 0.0 ? rest * b / bedarf : 0.0;
                    Ungedeckt(s.id, ungedeckt, EnergieflussForm.Waerme);
                    F(WAERME, s.id, b - entladung[k] - ungedeckt, EnergieflussForm.Waerme, EnergieflussHerkunft.Abgeleitet,
                      T(nameof(MyResource.Resource.EF_FORMEL_DIREKT)));
                }
            }

            /// <summary>
            /// Ermittelt die Quellentnahme je Wärmespeicher (Entladung ohne Kanalzeile) und
            /// liefert, wie viel davon gegen die Wärme der Wärmepumpe aufgerechnet wird —
            /// höchstens deren Produktion; die Ladung jedes Speichers sinkt um seinen Anteil.
            /// </summary>
            private double QuellentnahmenAufrechnen(double waermeWpMwh)
            {
                var roh = new Dictionary<ErgebnisPufferspeicherModel, double>();
                double summe = 0.0;
                foreach (ErgebnisPufferspeicherModel p in _e.Pufferspeicher ?? new List<ErgebnisPufferspeicherModel>())
                {
                    if (p == null || IstKaeltespeicher(p) || IstQuellspeicher(p)) continue;
                    double kanal = 0.0;
                    foreach (int k in Kanal.KANAELE_WAERME) kanal += Math.Max(0.0, p.Entladung_Kanal?[k] ?? 0.0);
                    double q = AusKwh(Math.Max(0.0, p.Entladung_gesamt - kanal));
                    if (q < MINDEST_MWH) continue;
                    roh[p] = q;
                    summe += q;
                }
                if (summe <= 0.0 || waermeWpMwh <= 0.0) return 0.0;
                double faktor = Math.Min(1.0, waermeWpMwh / summe);
                foreach (var kv in roh) _quellentnahme[kv.Key] = kv.Value * faktor;
                return summe * faktor;
            }

            private void Ungedeckt(string senke, double mwh, EnergieflussForm form)
            {
                if (mwh < MINDEST_MWH) return;
                KR(UNGEDECKT, nameof(MyResource.Resource.EF_KN_UNGEDECKT), nameof(MyResource.Resource.EF_KK_UNGEDECKT), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_UNGEDECKT);
                F(UNGEDECKT, senke, mwh, EnergieflussForm.Rest, EnergieflussHerkunft.Abgeleitet, T(nameof(MyResource.Resource.EF_FORMEL_UNGEDECKT)));
            }

            // ---------------------------------------------------------------
            //  Kälteseite: Kältespeicher, Kühlung, ungedeckt
            // ---------------------------------------------------------------

            private void Kaelteseite(ErgebnisEnergiebedarfModel eb)
            {
                double entladung = 0.0;
                var s = Senke(Kanal.KUEHLUNG);
                foreach (ErgebnisPufferspeicherModel p in _e.Pufferspeicher ?? new List<ErgebnisPufferspeicherModel>())
                {
                    if (p == null || !IstKaeltespeicher(p)) continue;
                    double ladung = AusKwh(p.Ladung_gesamt);
                    if (ladung < MINDEST_MWH) continue;
                    string id = KAELTESPEICHER + p.ID_Pufferspeicher.ToString(CultureInfo.InvariantCulture) + ":" + p.ID_Anlage.ToString(CultureInfo.InvariantCulture);
                    string name = string.IsNullOrWhiteSpace(p.Bezeichner) ? T(nameof(MyResource.Resource.EF_KN_KAELTESPEICHER)) : p.Bezeichner.Trim();
                    K(id, name, T(nameof(MyResource.Resource.EF_KK_KAELTESPEICHER)), EnergieflussKnotenart.Speicher, SPALTE_SPEICHER, R_KSPEICHER);
                    double kanal = Math.Max(0.0, p.Entladung_Kanal?[Kanal.KUEHLUNG] ?? 0.0);
                    double e = AusKwh(kanal > 0.0 ? kanal : p.Entladung_gesamt);
                    if (e >= MINDEST_MWH)
                    {
                        KR(s.id, s.name, s.kurz, EnergieflussKnotenart.Senke, SPALTE_SENKEN, s.rang, bedarf: true);
                        F(id, s.id, e, EnergieflussForm.Kaelte);
                        entladung += e;
                    }
                    Speicher(id, KAELTE, ladung, AusKwh(p.Verluste_gesamt), AusKwh(p.SOC_Ende), EnergieflussForm.Kaelte, R_KSPEICHER);
                }
                if (eb == null || !eb.KaelteErhoben) return;
                double b = Math.Max(0.0, eb.Kaeltebedarf_Gesamt ?? 0.0);
                if (b < MINDEST_MWH) return;
                KR(s.id, s.name, s.kurz, EnergieflussKnotenart.Senke, SPALTE_SENKEN, s.rang, bedarf: true);
                double ungedeckt = Math.Max(0.0, eb.Kaelterestbedarf ?? 0.0);
                Ungedeckt(s.id, ungedeckt, EnergieflussForm.Kaelte);
                F(KAELTE, s.id, b - entladung - ungedeckt, EnergieflussForm.Kaelte, EnergieflussHerkunft.Abgeleitet,
                  T(nameof(MyResource.Resource.EF_FORMEL_DIREKT)));
            }

            // ---------------------------------------------------------------
            //  Stromseite: Stromspeicher, Strombedarf, Einspeisung PV
            // ---------------------------------------------------------------

            private void Stromseite(ErgebnisEnergiebedarfModel eb)
            {
                double entladung = 0.0, einspeisungMit = 0.0;
                bool speicher = false;
                int i = 0;
                foreach (ErgebnisStromspeicherModel sp in _e.Stromspeicher ?? new List<ErgebnisStromspeicherModel>())
                {
                    i++;
                    if (sp == null) continue;
                    speicher = true;
                    einspeisungMit += AusKwh(sp.Einspeisung_Mit);
                    // EB-1: der Standby des Speichersystems (Standby aus PV und Netz, mit Flotte der Hilfsverbrauch der
                    // Einheiten) verlässt den Strombus als benanntes Verlustband — er ist Verbrauch des Systems, nicht
                    // Inhalt des Speichers (der Speicherknoten schließt über Ladung, Entladung, Verlust und Füllstand).
                    double standby = AusKwh(sp.EigenverbrauchSystemKwh ?? 0.0);
                    if (standby >= MINDEST_MWH)
                    {
                        string sid = STANDBY + i.ToString(CultureInfo.InvariantCulture);
                        KR(sid, nameof(MyResource.Resource.EF_KN_STANDBY), nameof(MyResource.Resource.EF_KK_STANDBY), EnergieflussKnotenart.Verlust, SPALTE_SPEICHER, R_SSPEICHER);
                        F(STROM, sid, standby, EnergieflussForm.Verlust);
                    }
                    double ladung = AusKwh(sp.Ladung_Gesamt);
                    if (ladung < MINDEST_MWH) continue;
                    string id = STROMSPEICHER + i.ToString(CultureInfo.InvariantCulture);
                    string name = string.IsNullOrWhiteSpace(sp.Bezeichner) ? T(nameof(MyResource.Resource.EF_KN_STROMSPEICHER)) : sp.Bezeichner.Trim();
                    K(id, name, T(nameof(MyResource.Resource.EF_KK_STROMSPEICHER)), EnergieflussKnotenart.Speicher, SPALTE_SPEICHER, R_SSPEICHER);
                    double e = AusKwh(sp.Entladung_Gesamt), v = AusKwh(sp.Verluste_Gesamt);
                    KR(STROMBEDARF, nameof(MyResource.Resource.EF_KN_STROMBEDARF), nameof(MyResource.Resource.EF_KK_STROMBEDARF), EnergieflussKnotenart.Senke, SPALTE_SENKEN, R_STROMBEDARF, bedarf: true);
                    F(id, STROMBEDARF, e, EnergieflussForm.Strom);
                    entladung += e >= MINDEST_MWH ? e : 0.0;
                    Speicher(id, STROM, ladung, v, ladung - e - v, EnergieflussForm.Strom, R_SSPEICHER);
                    _knoten[id].Zusatz += " · " + T(nameof(MyResource.Resource.EF_ZEIGER_STROMSPEICHER));
                }
                if (eb != null && eb.Strombedarf_Gesamt >= MINDEST_MWH)
                {
                    KR(STROMBEDARF, nameof(MyResource.Resource.EF_KN_STROMBEDARF), nameof(MyResource.Resource.EF_KK_STROMBEDARF), EnergieflussKnotenart.Senke, SPALTE_SENKEN, R_STROMBEDARF, bedarf: true);
                    F(STROM, STROMBEDARF, eb.Strombedarf_Gesamt - entladung, EnergieflussForm.Strom, entladung > 0.0 ? EnergieflussHerkunft.Abgeleitet : EnergieflussHerkunft.Direkt,
                      entladung > 0.0 ? T(nameof(MyResource.Resource.EF_FORMEL_DIREKT)) : "");
                }
                if (_e.Photovoltaik != null && _e.Photovoltaik.Stromproduktion >= MINDEST_MWH)
                {
                    // Mit Speicher die Einspeisung der Speicherzeile; rechnet ein BHKW mit, enthält sie auch dessen
                    // Einspeisung — dann höchstens der PV-Überschuss, der Rest wird am Stromknoten bilanziert.
                    double einsp = !speicher ? _e.Photovoltaik.Ueberschuss
                                 : _e.BHKW != null ? Math.Min(einspeisungMit, _e.Photovoltaik.Ueberschuss) : einspeisungMit;
                    if (einsp >= MINDEST_MWH)
                    {
                        KR(EINSPEISUNG_PV, nameof(MyResource.Resource.EF_KN_EINSPEISUNG_PV), nameof(MyResource.Resource.EF_KK_EINSPEISUNG_PV), EnergieflussKnotenart.Senke, SPALTE_SENKEN, R_EINSP_PV);
                        F(STROM, EINSPEISUNG_PV, einsp, EnergieflussForm.Strom);
                    }
                }
            }

            // ---------------------------------------------------------------
            //  Zuordnung: Erzeuger → Verwendung (Anwenderentscheid vom 17.10.2026)
            // ---------------------------------------------------------------

            /// <summary>
            /// <b>Die Wärmeseite der Darstellung Zuordnung.</b> Aus der fertigen Kette werden Wärmebus und Wärmespeicher
            /// herausgenommen; an ihre Stelle treten direkte Bänder Erzeuger → Verwendung. Die Menge je Band ist die Deckung des
            /// Erzeugers je Kanal aus dem Ergebnis — <c>Deckung_Kanal[k]</c> in % des Projektwärmebedarfs, also
            /// <c>Deckung_Kanal[k] / 100 · Waermebedarf_Gesamt</c> MWh (Aufschlüsselung der <c>Waermebedarfsdeckung</c>:
            /// Direktdeckung + zugerechnete Speicherentladung, bei der Wärmepumpe samt Heizstab). Was ein Erzeuger netto an
            /// den Wärmebus der Kette abgibt, ohne damit zu decken (Speicherverluste und Füllstand), steht als Band
            /// „Speicherverlust“ an ihm. Netto ist das Band Erzeuger → Wärmebus der Kette — bei der Wärmepumpe schon ohne die
            /// Quellentnahme der Booster-Kette (<see cref="QuellentnahmenAufrechnen"/>), die so nicht doppelt erscheint; der
            /// Heizstab gibt seine Wärme an die Wärmepumpe, deren Deckung ihn einschließt. Die Deckung des Heizkessels teilen
            /// Brennstoff- und Elektrokessel nach ihrer Wärmeabgabe. Ungedeckt, Eingangs-, Strom- und Kälteseite bleiben wie in
            /// der Kette. Ein Erzeuger, der mehr deckt, als er netto abgibt, schließt nicht und bekommt wie jeder offene Knoten
            /// sein Band „nicht zugeordnet“.
            /// </summary>
            private void WaermeZuordnen(ErgebnisEnergiebedarfModel eb)
            {
                _waermeBezugMwh = Zu(WAERME);
                double basis = Math.Max(0.0, eb?.Waermebedarf_Gesamt ?? 0.0);
                // Netto je Erzeuger: sein Band an den Wärmebus der Kette.
                var netto = new Dictionary<string, double>(StringComparer.Ordinal);
                foreach (EnergieflussFluss f in _fluesse.Where(f => f.Nach == WAERME))
                    netto[f.Von] = (netto.TryGetValue(f.Von, out double n) ? n : 0.0) + f.Mwh;
                double Netto(string id) => netto.TryGetValue(id, out double n) ? n : 0.0;

                // Wärmebus und Wärmespeicher verlassen das Bild samt aller ihrer Bänder.
                bool Weg(string id) => id == WAERME || id.StartsWith(PUFFER, StringComparison.Ordinal)
                                       || id.StartsWith(VERLUST + PUFFER, StringComparison.Ordinal)
                                       || id.StartsWith(FUELLSTAND + PUFFER, StringComparison.Ordinal);
                _fluesse.RemoveAll(f => Weg(f.Von) || Weg(f.Nach));

                // Die Wärmepumpe samt Heizstab ist EIN Erzeuger: Der Heizstab gibt seine Wärme an die Wärmepumpe.
                string wp = netto.ContainsKey(WP_HEIZEN) ? WP_HEIZEN : netto.ContainsKey(HEIZSTAB) ? HEIZSTAB : null;
                double wpNetto = Netto(WP_HEIZEN) + Netto(HEIZSTAB);
                if (wp == WP_HEIZEN) F(HEIZSTAB, WP_HEIZEN, Netto(HEIZSTAB), EnergieflussForm.Waerme);

                // Brennstoff- und Elektrokessel teilen die Deckung des Heizkessels nach ihrer Wärmeabgabe.
                double kessel = Netto(HEIZKESSEL), ekessel = Netto(ELEKTROKESSEL), kesselSumme = kessel + ekessel;

                if (wp != null) Zuordnen(wp, _e.Waermepumpe?.Deckung_Kanal, 1.0, wpNetto, basis);
                if (netto.ContainsKey(BHKW)) Zuordnen(BHKW, _e.BHKW?.Deckung_Kanal, 1.0, Netto(BHKW), basis);
                if (kesselSumme > 0.0)
                {
                    if (netto.ContainsKey(HEIZKESSEL)) Zuordnen(HEIZKESSEL, _e.Heizkessel?.Deckung_Kanal, kessel / kesselSumme, kessel, basis);
                    if (netto.ContainsKey(ELEKTROKESSEL)) Zuordnen(ELEKTROKESSEL, _e.Heizkessel?.Deckung_Kanal, ekessel / kesselSumme, ekessel, basis);
                }
                if (netto.ContainsKey(SOLARTHERMIE)) Zuordnen(SOLARTHERMIE, _e.Solarthermie?.Deckung_Kanal, 1.0, Netto(SOLARTHERMIE), basis);
            }

            /// <summary>
            /// Die Bänder eines Erzeugers in der Zuordnung: je Wärmekanal <c>Deckung_Kanal[k] / 100 · basis · anteil</c> in seine
            /// Verwendung, der Rest seiner Nettoabgabe als Speicherverlust.
            /// </summary>
            private void Zuordnen(string erzeuger, double[] deckungKanal, double anteil, double nettoMwh, double basis)
            {
                if (!_knoten.TryGetValue(erzeuger, out EnergieflussKnoten k)) return;
                double gedeckt = 0.0;
                foreach (int kanal in Kanal.KANAELE_WAERME)
                {
                    double d = deckungKanal != null && kanal < deckungKanal.Length ? deckungKanal[kanal] : 0.0;
                    if (!double.IsFinite(d) || d <= 0.0) continue;
                    double mwh = d / 100.0 * basis * anteil;
                    if (mwh < MINDEST_MWH) continue;
                    var s = Senke(kanal);
                    KR(s.id, s.name, s.kurz, EnergieflussKnotenart.Senke, SPALTE_SENKEN, s.rang, bedarf: true);
                    F(erzeuger, s.id, mwh, EnergieflussForm.Waerme, EnergieflussHerkunft.Abgeleitet, T(nameof(MyResource.Resource.EF_FORMEL_DECKUNG)));
                    gedeckt += mwh;
                }
                double verlust = nettoMwh - gedeckt;
                if (verlust < MINDEST_MWH) return;
                string vid = SPEICHERVERLUST + erzeuger;
                KR(vid, nameof(MyResource.Resource.EF_KN_SPEICHERVERLUST), nameof(MyResource.Resource.EF_KK_SPEICHERVERLUST),
                   EnergieflussKnotenart.Verlust, k.Spalte, k.Rang / 1000);
                F(erzeuger, vid, verlust, EnergieflussForm.Verlust, EnergieflussHerkunft.Abgeleitet, T(nameof(MyResource.Resource.EF_FORMEL_SPEICHERVERLUST)));
            }

            // ---------------------------------------------------------------
            //  Bilanz
            // ---------------------------------------------------------------

            /// <summary>
            /// Stromknoten: Mit erhobener Einspeisung der BHKW-Ergebniszeile (EB-1) ist das Band „Einspeisung BHKW“ die Zeile
            /// (Herkunft direkt). Die Zeile trägt den BHKW-Überschuss des Laufs — dieselbe Menge wie Reihe, Reiter und
            /// KWK-Split; ein Stromspeicher ohne Flotte lädt daraus seine <c>Ladung_BHKW</c>, ins Netz geht der Rest
            /// (Herkunft abgeleitet). Mit Flotte ist die Zeile schon die Netzeinspeisung der Flotte; welche der beiden
            /// Lesarten gilt, sagt der Knoten selbst: genommen wird die, mit der er schließt (zuerst die Zeile). Danach wird
            /// der Knoten wie jeder andere geprüft. Ohne Wert (Altergebnis, SK-Q1, 2.4 Fall 1) ist ein positiver Rest bis
            /// zum BHKW-Strom die bilanzierte Einspeisung.
            /// </summary>
            private void BilanzStrom()
            {
                if (_bhkwStromMwh >= MINDEST_MWH && _bhkwEinspeisungMwh.HasValue)
                {
                    double zeile = Math.Max(0.0, _bhkwEinspeisungMwh.Value);
                    double ladungBhkw = (_e.Stromspeicher ?? new List<ErgebnisStromspeicherModel>())
                        .Where(sp => sp != null).Sum(sp => Math.Max(0.0, AusKwh(sp.Ladung_BHKW)));
                    double offen = Zu(STROM) - Ab(STROM);
                    double band = zeile;
                    EnergieflussHerkunft herkunft = EnergieflussHerkunft.Direkt;
                    string formel = "";
                    if (ladungBhkw >= MINDEST_MWH && Math.Abs(offen - zeile) > Toleranz(Zu(STROM))
                        && Math.Abs(offen - (zeile - ladungBhkw)) <= Toleranz(Zu(STROM)))
                    {
                        band = zeile - ladungBhkw;
                        herkunft = EnergieflussHerkunft.Abgeleitet;
                        formel = T(nameof(MyResource.Resource.EF_FORMEL_EINSPEISUNG_BHKW));
                    }
                    if (band >= MINDEST_MWH)
                    {
                        KR(EINSPEISUNG_BHKW, nameof(MyResource.Resource.EF_KN_EINSPEISUNG_BHKW), nameof(MyResource.Resource.EF_KK_EINSPEISUNG_BHKW), EnergieflussKnotenart.Senke, SPALTE_SENKEN, R_EINSP_BHKW);
                        F(STROM, EINSPEISUNG_BHKW, band, EnergieflussForm.Strom, herkunft, formel);
                    }
                    Bilanz(STROM);
                    return;
                }
                double rest = Zu(STROM) - Ab(STROM);
                if (_bhkwStromMwh >= MINDEST_MWH && rest >= MINDEST_MWH && rest <= _bhkwStromMwh + Toleranz(Zu(STROM)))
                {
                    KR(EINSPEISUNG_BHKW, nameof(MyResource.Resource.EF_KN_EINSPEISUNG_BHKW), nameof(MyResource.Resource.EF_KK_EINSPEISUNG_BHKW), EnergieflussKnotenart.Senke, SPALTE_SENKEN, R_EINSP_BHKW);
                    F(STROM, EINSPEISUNG_BHKW, rest, EnergieflussForm.Strom, EnergieflussHerkunft.Bilanziert);
                    _knoten[STROM].RestMwh = rest;
                    return;
                }
                Bilanz(STROM);
            }

            /// <summary>
            /// Bilanz eines Knotens (2.4): innerhalb max(0,05 MWh; 0,5 %) geschlossen; darüber ein Band „nicht zugeordnet“
            /// (bei negativem Rest aus der Quellenspalte) und eine Hinweiszeile.
            /// </summary>
            private void Bilanz(string id)
            {
                if (!_knoten.TryGetValue(id, out EnergieflussKnoten k) || !Hat(id)) return;
                double zu = Zu(id), rest = zu - Ab(id);
                k.RestMwh = rest;
                if (Math.Abs(rest) <= Toleranz(zu)) return;
                k.Geschlossen = false;
                string formel = string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_NICHT_ZUGEORDNET)), k.Name);
                if (rest > 0.0)
                {
                    KR(NICHT_ZUGEORDNET, nameof(MyResource.Resource.EF_KN_NICHT_ZUGEORDNET), nameof(MyResource.Resource.EF_KK_NICHT_ZUGEORDNET), EnergieflussKnotenart.Senke, SPALTE_SENKEN, R_NZ);
                    F(id, NICHT_ZUGEORDNET, rest, EnergieflussForm.Rest, EnergieflussHerkunft.Bilanziert, formel);
                }
                else
                {
                    KR(NICHT_ZUGEORDNET_QUELLE, nameof(MyResource.Resource.EF_KN_NICHT_ZUGEORDNET), nameof(MyResource.Resource.EF_KK_NICHT_ZUGEORDNET), EnergieflussKnotenart.Quelle, SPALTE_QUELLEN, R_NZ_QUELLE);
                    F(NICHT_ZUGEORDNET_QUELLE, id, -rest, EnergieflussForm.Rest, EnergieflussHerkunft.Bilanziert, formel);
                }
                _m.Hinweise.Add(string.Format(_k, T(nameof(MyResource.Resource.EF_HINWEIS_BILANZ)), k.Name, Zahl(Math.Abs(rest), _k)));
            }

            // ---------------------------------------------------------------
            //  Bündelung (2.5)
            // ---------------------------------------------------------------

            /// <summary>
            /// Brennstoffträger, Split-Zeilen und Pufferspeicher: Die kleinsten Knoten einer Gruppe, die zusammen unter 1 %
            /// des zugehörigen Busses bleiben, werden zu „weitere (n)“ zusammengefasst, wenn es mehr als drei sind; sonst
            /// bleiben sie einzeln stehen. Ihre Verlust- und Füllstandsknoten gehen mit.
            /// </summary>
            private void Buendeln()
            {
                foreach (string gruppe in new[] { GRUPPE_TRAEGER, GRUPPE_SPLIT, GRUPPE_PUFFER })
                {
                    List<EnergieflussKnoten> glieder = _knoten.Values.Where(k => k.Gruppe == gruppe && Hat(k.Id)).ToList();
                    string bus = gruppe == GRUPPE_SPLIT ? KAELTE : WAERME;
                    double grenze = BUENDEL_ANTEIL * (bus == WAERME && double.IsFinite(_waermeBezugMwh) ? _waermeBezugMwh : Zu(bus));
                    double Menge(EnergieflussKnoten k) => Math.Max(Zu(k.Id), Ab(k.Id));
                    var klein = new List<EnergieflussKnoten>();
                    double summe = 0.0;
                    foreach (EnergieflussKnoten k in glieder.OrderBy(Menge).ThenBy(k => k.Rang))
                    {
                        double mk = Menge(k);
                        if (summe + mk >= grenze) break;
                        summe += mk;
                        klein.Add(k);
                    }
                    if (klein.Count <= BUENDEL_AB_ANZAHL) continue;

                    EnergieflussKnoten erster = klein.OrderBy(k => k.Rang).First();
                    string n = klein.Count.ToString(CultureInfo.InvariantCulture);
                    string bid = WEITERE + gruppe;
                    var b = K(bid, string.Format(_k, T(nameof(MyResource.Resource.EF_KN_WEITERE)), n),
                              string.Format(_k, T(nameof(MyResource.Resource.EF_KK_WEITERE)), n), erster.Art, erster.Spalte, erster.Rang / 1000, gruppe);
                    b.Rang = erster.Rang;
                    b.Gebuendelt = klein.OrderBy(k => k.Rang).Select(k => k.Name + " " + Zahl(Menge(k), _k)).ToList();
                    var abbildung = new Dictionary<string, string>();
                    foreach (EnergieflussKnoten k in klein)
                    {
                        abbildung[k.Id] = bid;
                        abbildung[VERLUST + k.Id] = VERLUST + bid;
                        abbildung[FUELLSTAND + k.Id] = FUELLSTAND + bid;
                        if (_knoten.TryGetValue(VERLUST + k.Id, out EnergieflussKnoten v))
                            K(VERLUST + bid, v.Name, v.Kurzname, v.Art, v.Spalte, v.Rang / 1000).Rang = v.Rang;
                        if (_knoten.TryGetValue(FUELLSTAND + k.Id, out EnergieflussKnoten f))
                            K(FUELLSTAND + bid, f.Name, f.Kurzname, f.Art, f.Spalte, f.Rang / 1000).Rang = f.Rang;
                        _knoten.Remove(k.Id);
                        _knoten.Remove(VERLUST + k.Id);
                        _knoten.Remove(FUELLSTAND + k.Id);
                    }
                    foreach (EnergieflussFluss fl in _fluesse)
                    {
                        if (abbildung.TryGetValue(fl.Von, out string von)) fl.Von = von;
                        if (abbildung.TryGetValue(fl.Nach, out string nach)) fl.Nach = nach;
                    }
                    // Gleiche Bänder zusammenlegen.
                    var zusammen = new List<EnergieflussFluss>();
                    foreach (EnergieflussFluss fl in _fluesse)
                    {
                        EnergieflussFluss da = zusammen.FirstOrDefault(z => z.Von == fl.Von && z.Nach == fl.Nach && z.Form == fl.Form);
                        if (da == null) { zusammen.Add(fl); continue; }
                        da.Mwh += fl.Mwh;
                        if (fl.Herkunft != EnergieflussHerkunft.Direkt && da.Herkunft == EnergieflussHerkunft.Direkt)
                        {
                            da.Herkunft = fl.Herkunft;
                            da.Formel = fl.Formel;
                        }
                    }
                    _fluesse.Clear();
                    _fluesse.AddRange(zusammen);
                }
            }

            // ---------------------------------------------------------------
            //  Abschluss: Mengen, Anteile, Zeigertexte, Reihenfolge
            // ---------------------------------------------------------------

            private void Abschliessen()
            {
                List<EnergieflussKnoten> knoten = _knoten.Values.Where(k => Hat(k.Id)).OrderBy(k => k.Spalte).ThenBy(k => k.Rang).ToList();
                var rang = knoten.Select((k, i) => (k.Id, i)).ToDictionary(x => x.Id, x => x.i);
                foreach (EnergieflussKnoten k in knoten)
                {
                    k.ZuflussMwh = Zu(k.Id);
                    k.AbflussMwh = Ab(k.Id);
                }
                foreach (EnergieflussFluss f in _fluesse)
                {
                    EnergieflussKnoten von = _knoten[f.Von], nach = _knoten[f.Nach];
                    bool amZiel = nach.IstBedarf || nach.Art == EnergieflussKnotenart.Wandler || nach.Art == EnergieflussKnotenart.Bus
                                  || nach.Art == EnergieflussKnotenart.Speicher;
                    double bezug = amZiel ? nach.ZuflussMwh : (von.Art == EnergieflussKnotenart.Quelle ? von.AbflussMwh : von.ZuflussMwh);
                    f.AnteilProzent = bezug > 0.0 ? 100.0 * f.Mwh / bezug : 0.0;
                    string z = string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_FLUSS)), von.Name, nach.Name, Zahl(f.Mwh, _k), Prozent(f.AnteilProzent, _k));
                    if (f.Herkunft == EnergieflussHerkunft.Bilanziert)
                        z += " · " + (string.IsNullOrEmpty(f.Formel) ? T(nameof(MyResource.Resource.EF_ZEIGER_BILANZIERT)) : f.Formel);
                    else if (f.Herkunft == EnergieflussHerkunft.Abgeleitet && !string.IsNullOrEmpty(f.Formel))
                        z += " · " + string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_ABGELEITET)), f.Formel);
                    f.Zeigertext = z;
                }
                foreach (EnergieflussKnoten k in knoten)
                {
                    string z = k.Art == EnergieflussKnotenart.Quelle
                        ? string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_MENGE)), k.Name, Zahl(k.AbflussMwh, _k))
                        : k.Art == EnergieflussKnotenart.Senke || k.Art == EnergieflussKnotenart.Verlust
                            ? string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_MENGE)), k.Name, Zahl(k.ZuflussMwh, _k))
                            : string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_KNOTEN)), k.Name, Zahl(k.ZuflussMwh, _k), Zahl(k.AbflussMwh, _k));
                    if (!string.IsNullOrEmpty(k.Zusatz)) z += " · " + k.Zusatz;
                    if (k.Gebuendelt.Count > 0)
                        z += " · " + string.Format(_k, T(nameof(MyResource.Resource.EF_ZEIGER_GEBUENDELT)), string.Join(", ", k.Gebuendelt));
                    k.Zeigertext = z;
                }
                _m.Knoten.AddRange(knoten);
                _m.Fluesse.AddRange(_fluesse.OrderBy(f => rang[f.Von]).ThenBy(f => rang[f.Nach]));
            }
        }
    }
}
