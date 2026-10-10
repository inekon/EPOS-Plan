using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Stufen der Kältefolge in einer Kältestunde (Entwurf Kältebereich 1.4, 3.4): erst die freie Kühlung
    /// der Kältemaschinen mit Trocken- oder Nasskühler, dann die Entladung der Kältespeicher, dann die
    /// Wärmepumpen im Kühlbetrieb, zuletzt die Kältemaschinen.
    /// </summary>
    public enum KaeltefolgeStufe
    {
        /// <summary>Kältemaschinen mit Trocken- oder Nasskühler in Stunden mit kaltem Rückkühler — vor allen anderen.</summary>
        FreieKuehlung = 0,

        /// <summary>Die Kältespeicher entladen, geordnet nach Entladepriorität (0 = automatisch, hinten).</summary>
        Kaeltespeicher = 1,

        /// <summary>Die Wärmepumpen im Kühlbetrieb in der Folge ihrer Module (Wärmekaskade, Anlagenpriorität, ID).</summary>
        Waermepumpe = 2,

        /// <summary>Die Kältemaschinen in der Folge ihrer Anlagen-ID.</summary>
        Kaeltemaschine = 3
    }

    /// <summary>Ein Kälteerzeuger des Projekts in der Kältefolge, wie ihn der Bereich „Kälte“ zeigt (<see cref="Kaeltefolge.Lesen"/>).</summary>
    public sealed class KaelteerzeugerEintrag
    {
        /// <summary><see cref="KaeltefolgeStufe.Waermepumpe"/> oder <see cref="KaeltefolgeStufe.Kaeltemaschine"/>.</summary>
        public KaeltefolgeStufe Art;

        /// <summary><c>Tab_Energieanlagen.ID</c>.</summary>
        public int AnlagenId;

        /// <summary>Die Projektkopie des Geräts: <c>Tab_WP.ID</c> bzw. <c>Tab_Kaeltemaschine.ID</c>; 0 = keine.</summary>
        public int IdGeraet;

        /// <summary>Der Anzeigename der Anlage.</summary>
        public string Bezeichner = "";

        /// <summary>Anzahl gleicher Geräte (Kältemaschine; die Wärmepumpe immer 1).</summary>
        public int Anzahl = 1;

        /// <summary>Nennkälteleistung je Gerät [kW] (<c>Tab_WP.Kuehlleistung</c> bzw. <c>Nennkaelteleistung_kW</c>); <c>null</c> = ungepflegt.</summary>
        public double? NennleistungKw;

        /// <summary>Kühlbetrieb an? Die Kältemaschine immer; die Wärmepumpe nach <c>Tab_WP.Kuehlbetrieb</c>.</summary>
        public bool Kuehlbetrieb;

        /// <summary>Warum sich der Kühlbetrieb der Wärmepumpe nicht einschalten lässt; <c>null</c> = frei (Kältemaschine immer <c>null</c>).</summary>
        public string Sperrgrund;

        /// <summary>Wärmepumpe: steht die Erzeugerart auf einem Platz der Wärmekaskade (<c>Tool_1…4</c>)? Ohne Platz kühlt sie nicht.</summary>
        public bool InWaermekaskade = true;

        /// <summary>Kühl- bzw. Kaltwasservorlauf [°C] der Projektkopie; <c>null</c> = Vorgabe des Geräts.</summary>
        public double? KuehlVorlaufC;

        /// <summary>Hilfsstromanteil (0…1) der Projektkopie; <c>null</c> = kein Zuschlag.</summary>
        public double? Hilfsstromanteil;

        /// <summary>Kühlträger der Anlagenzeile (<c>Kuehl_ID_Carrier</c>); <c>null</c> = Stromträger des Projekts.</summary>
        public int? KuehltraegerId;

        /// <summary>Name des Kühlträgers; leer ohne Kühlträger.</summary>
        public string KuehltraegerName = "";

        /// <summary>Abrechnung über einen eigenen Zähler (<c>Kuehl_EigenerZaehler</c>).</summary>
        public bool EigenerZaehler;

        /// <summary>
        /// Freie Kühlung: Kältemaschine mit Trocken- oder Nasskühler (Rückkühlart); Wärmepumpe mit Schalter
        /// <c>Kuehl_Frei</c> und einer Quelle, die ihn trägt (Sole/Erdreich).
        /// </summary>
        public bool FreieKuehlung;

        /// <summary>Kältemaschine: die Rückkühlart als Persistenzwert; leer bei der Wärmepumpe.</summary>
        public string Rueckkuehlart = "";

        /// <summary>Der Platz innerhalb der Stufe: Modulindex der Wärmepumpe bzw. Listenplatz der Kältemaschine.</summary>
        public int Platz;

        /// <summary>
        /// Wie viele Anlagen dieselbe Projektkopie führen (Kältemaschine; Bestand vor KB-1 oder ein Gerät, das mehrere
        /// Wärmepumpen-Anlagen tragen). Größer 1 heißt: Vorlauf und Hilfsstrom gelten für alle diese Anlagen.
        /// </summary>
        public int AnlagenJeKopie = 1;

        /// <summary>
        /// Der gepflegte Rang in der Kältefolge (<c>Tab_Energieanlagen.Kaelte_Rang</c>, Welle KB-D); <c>null</c> = kein Rang,
        /// der Erzeuger steht nach der Vorgabefolge (hinter allen gepflegten).
        /// </summary>
        public int? KaelteRang;
    }

    /// <summary>Ein Eintrag der freien Kühlung (<see cref="KaeltefolgeStand.FreieKuehlung"/>).</summary>
    /// <param name="Art">Der Erzeuger, der frei kühlt.</param>
    /// <param name="AnlagenId"><c>Tab_Energieanlagen.ID</c>.</param>
    /// <param name="Bezeichner">Der Anzeigename.</param>
    /// <param name="VorAllenErzeugern">
    /// <c>true</c> = Kältemaschine: deckt in Stunden mit kaltem Rückkühler vor allen anderen; <c>false</c> =
    /// Wärmepumpe: kühlt an ihrem eigenen Platz frei, vor dem Verdichter.
    /// </param>
    public sealed record FreieKuehlungEintrag(KaeltefolgeStufe Art, int AnlagenId, string Bezeichner, bool VorAllenErzeugern);

    /// <summary>Der Stand der Kälteseite eines Projekts in der Folge des Laufs (<see cref="Kaeltefolge.Lesen"/>).</summary>
    public sealed class KaeltefolgeStand
    {
        /// <summary>Die Projekteinstellung „Kühlung rechnen“ (<c>Tab_Einstellungen.Kuehlbetrieb</c>).</summary>
        public bool Kuehlbetrieb;

        /// <summary>Die Stufen in ihrer Folge — fest (<see cref="Kaeltefolge.Stufen"/>); gepflegt wird die Folge der Erzeuger.</summary>
        public IReadOnlyList<KaeltefolgeStufe> Stufen = Kaeltefolge.Stufen;

        /// <summary>Die Kälteerzeuger in Rechenfolge: Wärmepumpen mit Kühlfunktion, dann Kältemaschinen.</summary>
        public IReadOnlyList<KaelteerzeugerEintrag> Erzeuger = Array.Empty<KaelteerzeugerEintrag>();

        /// <summary>Die Kältespeicher (Puffer mit Verwendung Kälte) in Entladefolge.</summary>
        public IReadOnlyList<WaermesenkeClass.PufferInfo> Kaeltespeicher = Array.Empty<WaermesenkeClass.PufferInfo>();

        /// <summary>Die Erzeuger mit freier Kühlung: zuerst die Kältemaschinen (vor allen), dann die Wärmepumpen.</summary>
        public IReadOnlyList<FreieKuehlungEintrag> FreieKuehlung = Array.Empty<FreieKuehlungEintrag>();

        /// <summary>
        /// Ist die Folge der Erzeuger gepflegt (mindestens ein Erzeuger mit <see cref="KaelteerzeugerEintrag.KaelteRang"/>)?
        /// <c>false</c> = die Vorgabefolge gilt.
        /// </summary>
        public bool Gepflegt;
    }

    /// <summary>
    /// <b>Die Kältefolge — eine Quelle der Wahrheit</b> (Entwurf Kältebereich 3.4, Welle KB-A): Die Ordnungsregeln
    /// der Kälteseite stehen hier einmal, und der Lauf (<c>SimulationControl.KaelteerzeugerVorbereiten</c>,
    /// <c>KaeltemaschinenVorbereiten</c>, <c>KaeltespeicherLesen</c>), das Schema (<c>SchemaModell.KaelteBahnAnlegen</c>)
    /// und der Bereich „Kälte“ der Simulationskonfiguration (<see cref="Lesen"/>) ordnen damit.
    /// <para><b>Pflegbar (Welle KB-D, Entscheid E117 F1):</b> Die Stufen bleiben fest — freie Kühlung, Kältespeicher (nach
    /// Entladepriorität), Erzeuger. Die Folge der Erzeuger trägt ein Rang an der Anlagenzeile
    /// (<c>Tab_Energieanlagen.Kaelte_Rang</c>, <see cref="RaengeLesen"/>): Erzeuger mit Rang stehen vorn, aufsteigend; ohne
    /// Rang folgen sie in der Vorgabefolge (Wärmepumpen nach Modulfolge, dann Kältemaschinen nach Anlagen-ID). Ohne jeden
    /// Rang ist die Folge Zeichen für Zeichen die Vorgabefolge. Geschrieben wird der Rang allein über
    /// <see cref="KaeltefolgeCtrl"/>.</para>
    /// </summary>
    public static class Kaeltefolge
    {
        /// <summary>Die feste Folge der Stufen.</summary>
        public static readonly IReadOnlyList<KaeltefolgeStufe> Stufen = new[]
        {
            KaeltefolgeStufe.FreieKuehlung, KaeltefolgeStufe.Kaeltespeicher,
            KaeltefolgeStufe.Waermepumpe, KaeltefolgeStufe.Kaeltemaschine
        };

        /// <summary>Der Rang einer Stufe in <see cref="Stufen"/>.</summary>
        public static int Rang(KaeltefolgeStufe stufe)
        {
            for (int i = 0; i < Stufen.Count; i++) if (Stufen[i] == stufe) return i;
            return int.MaxValue;
        }

        // =====================================================================
        //  Die Ordnungsregeln - Lauf, Schema und Anzeige rufen sie
        // =====================================================================

        /// <summary>
        /// Ordnet Kälteerzeuger, stabil (gleicher Schlüssel = Eingangsfolge): zuerst die mit gepflegtem Rang
        /// (<paramref name="rang"/>, aufsteigend), dann die ohne Rang nach Stufe und Platz in der Stufe (Vorgabefolge).
        /// <paramref name="rang"/> <c>null</c> oder ohne Wert für alle = die Vorgabefolge.
        /// </summary>
        public static List<T> ErzeugerOrdnen<T>(IEnumerable<T> erzeuger, Func<T, KaeltefolgeStufe> stufe, Func<T, int> platz,
                                                Func<T, int?> rang = null)
        {
            if (erzeuger == null) return new List<T>();
            return erzeuger.Select((e, i) => (e, i, r: rang?.Invoke(e)))
                           .OrderBy(t => t.r.HasValue ? 0 : 1)
                           .ThenBy(t => t.r ?? 0)
                           .ThenBy(t => Rang(stufe(t.e)))
                           .ThenBy(t => platz(t.e))
                           .ThenBy(t => t.i)
                           .Select(t => t.e).ToList();
        }

        /// <summary>
        /// Die gepflegten Ränge der Anlagen eines Projekts (<c>Tab_Energieanlagen.Kaelte_Rang</c>): Anlagen-ID → Rang, nur
        /// Zeilen mit Rang. Leer ohne Projekt, ohne Spalte (Stand vor Schritt <see cref="KaelteRangSchema.SCHRITT"/>) oder
        /// ohne gepflegten Rang.
        /// </summary>
        public static Dictionary<int, int> RaengeLesen(int idProjekt)
        {
            var raenge = new Dictionary<int, int>();
            if (idProjekt <= 0 || !KaelteRangSchema.Vollstaendig()) return raenge;
            DataTable dt = StilleDb.Tabelle(
                "SELECT ID, Kaelte_Rang FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND Kaelte_Rang IS NOT NULL",
                StilleDb.Par("@p", DbParamTyp.Integer, idProjekt));
            if (dt == null) return raenge;
            foreach (DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID"));
                int rang = StilleDb.Zahl(StilleDb.Feld(r, KaelteRangSchema.SPALTE_RANG));
                if (id > 0 && rang >= 1) raenge[id] = rang;
            }
            return raenge;
        }

        /// <summary>Der Rang einer Anlage aus <paramref name="raenge"/>; <c>null</c> = keiner.</summary>
        private static int? RangVon(IReadOnlyDictionary<int, int> raenge, int anlagenId)
            => raenge != null && raenge.TryGetValue(anlagenId, out int r) ? r : (int?)null;

        /// <summary>
        /// Die Erzeuger des Laufs in Kältefolge: die gepflegten nach Rang (<paramref name="raenge"/>, Anlagen-ID → Rang),
        /// dann Wärmepumpen nach Modulindex und Kältemaschinen in der Folge, in der <see cref="KaeltemaschinenOrdnen"/> sie
        /// geliefert hat. Ohne Ränge die Vorgabefolge.
        /// </summary>
        internal static List<Kaelteerzeuger> ErzeugerOrdnen(IList<Kaelteerzeuger> erzeuger, IReadOnlyDictionary<int, int> raenge = null)
        {
            if (erzeuger == null) return new List<Kaelteerzeuger>();
            var listenplatz = new Dictionary<Kaelteerzeuger, int>();
            for (int i = 0; i < erzeuger.Count; i++) listenplatz[erzeuger[i]] = i;
            return ErzeugerOrdnen(erzeuger,
                e => e.Maschine != null ? KaeltefolgeStufe.Kaeltemaschine : KaeltefolgeStufe.Waermepumpe,
                e => e.Maschine != null ? listenplatz[e] : e.Modulindex,
                e => RangVon(raenge, e.AnlagenID));
        }

        /// <summary>Die Kältemaschinen-Anlagen in Kältefolge: nach Anlagen-ID.</summary>
        public static List<KaeltemaschineAnlageModel> KaeltemaschinenOrdnen(IEnumerable<KaeltemaschineAnlageModel> anlagen)
            => anlagen == null
                ? new List<KaeltemaschineAnlageModel>()
                : anlagen.Where(a => a != null).OrderBy(a => a.AnlagenId).ToList();

        /// <summary>
        /// Die Kältespeicher in Entladefolge: gepflegte Entladepriorität vorn (aufsteigend), 0 (automatisch) hinten,
        /// sonst die Eingangsfolge (<c>ProjektPufferListe</c>).
        /// </summary>
        public static List<T> KaeltespeicherOrdnen<T>(IEnumerable<T> speicher, Func<T, int> entladeprio)
        {
            if (speicher == null) return new List<T>();
            return speicher.Select((sp, i) => (sp, i))
                           .OrderBy(t => entladeprio(t.sp) > 0 ? entladeprio(t.sp) : int.MaxValue)
                           .ThenBy(t => t.i)
                           .Select(t => t.sp).ToList();
        }

        // =====================================================================
        //  Der Leser für den Bereich „Kälte“
        // =====================================================================

        /// <summary>
        /// <b>Der Stand der Kälteseite eines Projekts</b> in der Folge des Laufs: die Wärmepumpen mit Kühlfunktion (je
        /// Anlage in Modulfolge; ein Gerät ohne Kühlkennlinie, das keine nachholen kann und nicht im Kühlbetrieb
        /// steht, fehlt), die Kältemaschinen-Anlagen, die Kältespeicher und die freie Kühlung. Dialogfrei; ohne
        /// Projekt der leere Stand.
        /// </summary>
        public static KaeltefolgeStand Lesen(int idProjekt)
        {
            var stand = new KaeltefolgeStand();
            if (idProjekt <= 0) return stand;

            stand.Kuehlbetrieb = KonfigurationCtrl.KuehlbetriebLesen(idProjekt);
            var erzeuger = new List<KaelteerzeugerEintrag>();
            erzeuger.AddRange(WaermepumpenLesen(idProjekt));
            erzeuger.AddRange(KaeltemaschinenLesen(idProjekt));
            Dictionary<int, int> raenge = RaengeLesen(idProjekt);
            foreach (KaelteerzeugerEintrag e in erzeuger) e.KaelteRang = RangVon(raenge, e.AnlagenId);
            stand.Erzeuger = ErzeugerOrdnen(erzeuger, e => e.Art, e => e.Platz, e => e.KaelteRang);
            stand.Gepflegt = erzeuger.Any(e => e.KaelteRang.HasValue);

            stand.Kaeltespeicher = KaeltespeicherOrdnen(
                (WaermesenkeClass.ProjektPufferListe(idProjekt, WaermesenkeClass.VERWENDUNG_KAELTE)
                 ?? new List<WaermesenkeClass.PufferInfo>()).Where(p => p != null),
                p => p.Entladeprio);

            stand.FreieKuehlung = stand.Erzeuger
                .Where(e => e.FreieKuehlung)
                .OrderBy(e => e.Art == KaeltefolgeStufe.Kaeltemaschine ? 0 : 1)
                .Select(e => new FreieKuehlungEintrag(e.Art, e.AnlagenId, e.Bezeichner, e.Art == KaeltefolgeStufe.Kaeltemaschine))
                .ToList();
            return stand;
        }

        /// <summary>Die Wärmepumpen-Anlagen mit Kühlfunktion in Modulfolge.</summary>
        private static List<KaelteerzeugerEintrag> WaermepumpenLesen(int idProjekt)
        {
            var liste = new List<KaelteerzeugerEintrag>();
            List<int> module = SimulationControl.WaermepumpenanlagenLesen(idProjekt);
            if (module == null || module.Count == 0) return liste;

            Dictionary<int, WErzeugerModel> modelle = new Dictionary<int, WErzeugerModel>();
            foreach (WErzeugerModel m in WErzeugerCtrl.ModelleJeTyp(idProjekt, WizardItemClass.WP_TYP))
                if (m != null) modelle[m.ID] = m;
            Dictionary<int, WPCtrl.KuehlfaehigesGeraet> kuehlfaehig = WPCtrl.KuehlfaehigeGeraete(idProjekt)
                .GroupBy(g => g.IdWp).ToDictionary(g => g.Key, g => g.First());
            bool inKaskade = Kaskade.Lesen(KonfigurationCtrl.LiesProjektOhneNachziehen(idProjekt))
                .Contains(DbWerte.ERZEUGER_WAERMEPUMPE);

            Dictionary<int, int> jeGeraet = module.Where(modelle.ContainsKey).GroupBy(id => modelle[id].ID_WP)
                .ToDictionary(g => g.Key, g => g.Count());

            for (int i = 0; i < module.Count && i < SimulationWaermepumpe.MAX_WP; i++)
            {
                if (!modelle.TryGetValue(module[i], out WErzeugerModel m) || m.ID_WP <= 0) continue;
                DataTable dt = StilleDb.Tabelle(
                    "SELECT Kuehlbetrieb, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil, Kuehlleistung, Typ FROM Tab_WP WHERE ID = ?",
                    StilleDb.Par("@id", DbParamTyp.Integer, m.ID_WP));
                if (dt == null || dt.Rows.Count == 0) continue;
                DataRow r = dt.Rows[0];
                bool kuehlbetrieb = StilleDb.Zahl(StilleDb.Feld(r, "Kuehlbetrieb")) != 0;
                kuehlfaehig.TryGetValue(m.ID_WP, out WPCtrl.KuehlfaehigesGeraet geraet);
                if (geraet == null && !kuehlbetrieb) continue;

                object vorlauf = StilleDb.Feld(r, "Kuehl_Vorlauf");
                object hilfs = StilleDb.Feld(r, "Kuehl_Hilfsstromanteil");
                object leistung = StilleDb.Feld(r, "Kuehlleistung");
                string wpTyp = StilleDb.Feld(r, "Typ") as string ?? "";
                string wqTyp = WaermequelleClass.WertLesenStill(m.ID, "WQ_Typ") as string;
                int? traeger = m.Kuehl_ID_Carrier.HasValue && m.Kuehl_ID_Carrier.Value > 0 ? m.Kuehl_ID_Carrier : null;

                liste.Add(new KaelteerzeugerEintrag
                {
                    Art = KaeltefolgeStufe.Waermepumpe,
                    AnlagenId = m.ID,
                    IdGeraet = m.ID_WP,
                    Bezeichner = string.IsNullOrEmpty(m.Bezeichner)
                        ? m.ID.ToString(System.Globalization.CultureInfo.CurrentCulture) : m.Bezeichner,
                    Anzahl = 1,
                    NennleistungKw = leistung != null && StilleDb.Kommazahl(leistung) > 0 ? StilleDb.Kommazahl(leistung) : (double?)null,
                    Kuehlbetrieb = kuehlbetrieb,
                    Sperrgrund = geraet?.Sperrgrund,
                    InWaermekaskade = inKaskade,
                    KuehlVorlaufC = vorlauf != null ? StilleDb.Kommazahl(vorlauf) : (double?)null,
                    Hilfsstromanteil = hilfs != null ? StilleDb.Kommazahl(hilfs) : (double?)null,
                    KuehltraegerId = traeger,
                    KuehltraegerName = traeger.HasValue ? Emissionsquelle.TraegerName(traeger.Value) ?? "" : "",
                    EigenerZaehler = traeger.HasValue && m.Kuehl_EigenerZaehler == true,
                    FreieKuehlung = m.Kuehl_Frei && SimulationControl.FreieKuehlungSoleMoeglich(wpTyp, wqTyp),
                    Platz = i,
                    AnlagenJeKopie = jeGeraet.TryGetValue(m.ID_WP, out int n) ? n : 1
                });
            }
            return liste;
        }

        /// <summary>Die Kältemaschinen-Anlagen in Kältefolge samt den Werten ihrer Projektkopie.</summary>
        private static List<KaelteerzeugerEintrag> KaeltemaschinenLesen(int idProjekt)
        {
            var liste = new List<KaelteerzeugerEintrag>();
            List<KaeltemaschineAnlageModel> anlagen = KaeltemaschinenOrdnen(KaeltemaschineAnlageCtrl.ListeStill(idProjekt));
            Dictionary<int, int> jeKopie = anlagen.Where(a => a.IdKaeltemaschine.HasValue)
                .GroupBy(a => a.IdKaeltemaschine.Value).ToDictionary(g => g.Key, g => g.Count());
            for (int i = 0; i < anlagen.Count; i++)
            {
                KaeltemaschineAnlageModel a = anlagen[i];
                KaeltemaschineModel m = a.IdKaeltemaschine.HasValue ? KaeltemaschineCtrl.LadenStill(a.IdKaeltemaschine.Value) : null;
                int? traeger = a.KuehlIdCarrier.HasValue && a.KuehlIdCarrier.Value > 0 ? a.KuehlIdCarrier : null;
                string rueckkuehlart = m?.Rueckkuehlart ?? "";
                liste.Add(new KaelteerzeugerEintrag
                {
                    Art = KaeltefolgeStufe.Kaeltemaschine,
                    AnlagenId = a.AnlagenId,
                    IdGeraet = a.IdKaeltemaschine ?? 0,
                    Bezeichner = !string.IsNullOrWhiteSpace(a.Bezeichner) ? a.Bezeichner
                               : m != null && !string.IsNullOrWhiteSpace(m.Bezeichner) ? m.Bezeichner
                               : a.AnlagenId.ToString(System.Globalization.CultureInfo.CurrentCulture),
                    Anzahl = Math.Max(1, a.Anzahl),
                    NennleistungKw = m?.Nennkaelteleistung_kW,
                    Kuehlbetrieb = true,
                    KuehlVorlaufC = a.KuehlVorlauf,
                    Hilfsstromanteil = a.KuehlHilfsstromanteil,
                    KuehltraegerId = traeger,
                    KuehltraegerName = traeger.HasValue ? Emissionsquelle.TraegerName(traeger.Value) ?? "" : "",
                    EigenerZaehler = traeger.HasValue && a.KuehlEigenerZaehler == true,
                    FreieKuehlung = rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER ||
                                    rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER,
                    Rueckkuehlart = rueckkuehlart,
                    Platz = i,
                    AnlagenJeKopie = a.IdKaeltemaschine.HasValue ? jeKopie[a.IdKaeltemaschine.Value] : 1
                });
            }
            return liste;
        }
    }
}
