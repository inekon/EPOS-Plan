using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle des Erzeugerdialogs „Kältemaschinen im Projekt"</b> (KU3-4c) — sie baut aus
    /// <see cref="KaeltemaschineAnlageCtrl"/> den Parametersatz der Razor-Komponente
    /// <c>KaeltemaschineAnlageDialog</c> und bildet zwischen <see cref="KaeltemaschineAnlageModel"/> und
    /// <see cref="KaeltemaschineAnlageDaten"/> ab. Plattformfrei: Unter Windows reicht die Hauptfensterhülle den
    /// Satz an die <c>AppWurzel</c> (Seitenschlüssel <c>KAELTEMASCHINE_ANLAGE</c>), auf iOS die Projektquelle.
    ///
    /// <para><b>Anlegen</b> nimmt den Weg des Kerns (<see cref="KaeltemaschineAnlageCtrl.Anlegen"/>: eigene
    /// Projektkopie je Anlage über <c>EigeneKopieAnlegen</c>, Festlegung KB-1, und Anlagenzeile Typ 13); <b>Speichern</b> liest die Anlage neu, setzt die
    /// Eingaben und schreibt Anlagenzeile und Kühleingaben der Kopie in einem Vorgang.</para>
    /// </summary>
    internal static class KaeltemaschineAnlageHuelle
    {
        /// <summary>Der Parametersatz für ein Projekt; <c>null</c> ohne Projekt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int projektId)
        {
            if (projektId <= 0) return null;
            var traeger = Kaeltestromabrechnung.StromtraegerDesProjekts(projektId)
                .Select(t => (t.Key, t.Value)).ToList();
            // K-F5c: der Stand des Rückkühlwerks je Anlage vor dem ersten Schreiben dieser Dialogsitzung.
            var sitzung = new Rueckkuehlwerksitzung();
            return new Dictionary<string, object>
            {
                ["Anlagen"] = new Func<IReadOnlyList<KaeltemaschineAnlageDaten>>(() => Liste(projektId)),
                // KM-K1 (KA-E-26): Kältemaschinen und Katalog-Wärmepumpen mit Kühlkennlinie in einer Liste mit Spalte „Art“.
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(KuehlerzeugerKatalogCtrl.Katalogzeilen),
                ["Katalogprofil"] = KuehlerzeugerKatalogCtrl.Profil(Katalogtexte.Fuer),
                ["Katalogwerte"] = new Func<int, KaeltemaschineGeraetwerte>(id => Werte(KaeltemaschineStammCtrl.Laden(id))),
                ["Stromtraeger"] = (IReadOnlyList<(int Id, string Text)>)traeger,
                ["ProjektStromtraeger"] = Kaeltestromabrechnung.Projekttraeger(projektId),
                ["KuehltraegerVorauswahl"] = (int?)Vorauswahl(projektId),
                ["Pruefen"] = new Func<KaeltemaschineAnlageDaten, string>(Pruefen),
                ["Anlegen"] = new Func<int, string, int>((stammId, name) => KaeltemaschineAnlageCtrl.Anlegen(projektId, stammId, name)),
                ["Speichern"] = new Func<KaeltemaschineAnlageDaten, string>(d => Speichern(d, sitzung)),
                ["Loeschen"] = new Action<int>(KaeltemaschineAnlageCtrl.Loeschen),
                // KT: "Typkennfelder laden..." in der Katalogwahl - derselbe Weg wie in der Verwaltung.
                ["TypkennfelderLaden"] = new Func<KaeltemaschineTypkennfelderErgebnis>(KaeltemaschineKatalogHuelle.TypkennfelderLaden),
                // KM-K1 (KA-E-26): Wärmepumpen mit Kühlfunktion — Projektliste, Übernehmen (Kopie und Anlage über den
                // Anlegeweg der Erzeuger, Kühlbetrieb allein über WPCtrl.KuehlkonfigurationSchreiben), Ausschalten,
                // Zurücknehmen und die Ränge der Kältefolge.
                ["KuehlWaermepumpen"] = new Func<IReadOnlyList<KuehlWaermepumpeZeile>>(() => Waermepumpen(projektId)),
                ["WaermepumpeUebernehmen"] = new Func<int, KuehlWpUebernahme>(stamm => WaermepumpeUebernehmen(projektId, stamm)),
                ["WpKuehlbetriebSetzen"] = new Func<int, bool, string>(
                    (idWp, an) => KuehlerzeugerKatalogCtrl.KuehlbetriebSetzen(projektId, idWp, an)),
                ["WpUebernahmeZuruecknehmen"] = new Action<object>(m =>
                    KuehlerzeugerKatalogCtrl.UebernahmeZuruecknehmen(projektId, m as KuehlerzeugerKatalogCtrl.Uebernahme)),
                ["KaelteRaenge"] = new Func<IReadOnlyDictionary<int, int>>(() => KuehlerzeugerKatalogCtrl.Raenge(projektId)),
                ["KaelteRaengeWiederherstellen"] = new Func<IReadOnlyDictionary<int, int>, string>(
                    r => KaeltefolgeCtrl.RaengeWiederherstellen(projektId, r)),
                // KM-K2: der Bearbeitungsweg einer Wärmepumpen-Anlagenzeile (Naht WaermepumpeAnlageGaben) -
                // plattformfrei über WaermepumpeAnlagenaht; ohne Anlagenzeile null, dann bleibt die Sperre.
                // KM-K2: die Verwendungsmarke der Wärmepumpe je Projektkopie (nicht erst nach Kühlbetrieb).
                ["WaermepumpeKopien"] = new Func<IReadOnlyList<string>>(() => KuehlerzeugerKatalogCtrl.ProjektkopienNamen(projektId)),
                ["WaermepumpeAnlageGaben"] = new Func<int, WaermepumpeAnlagegaben>(
                    idWp => WaermepumpeAnlagenaht.Gaben(projektId, idWp)),
                ["SchlossWaermepumpe"] = Schlosswege.Aus(WPStammCtrl.SchlossSetzen),
                ["WpKatalogLoeschen"] = new Func<int, string>(WpKatalogLoeschen),
                ["VergleichsparameterWp"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    n => ParameterUebersichtCtrl.Werte(Anlagenart.Waermepumpe, n, Katalogtexte.Fuer)),
                ["VergleichsparameterWpProjekt"] = new Func<int, IReadOnlyList<Parameterwert>>(
                    id => ParameterUebersichtCtrl.ProjektWerte(Anlagenart.Waermepumpe, id, Katalogtexte.Fuer)),

                // KATALOGAUSWAHL V1, STUFE 5 (Konzept 4.9, 5.10): die Wege der Zweispaltenauswahl - Vergleich und
                // Schloss wie in der Verwaltung, Loeschen ueber den Kernweg, der Katalogeditor als Ueberlagerung
                // (die Windows-Schale reicht die Gaben samt Import herein, KaeltemaschineAnlageFensterHuelle).
                ["Vergleichsparameter"] = new Func<string, IReadOnlyList<Parameterwert>>(KaeltemaschineParameteruebersicht.Werte),
                ["VergleichsparameterProjekt"] = new Func<int, IReadOnlyList<Parameterwert>>(KaeltemaschineParameteruebersicht.ProjektWerte),
                ["Schloss"] = Schlosswege.Aus(KaeltemaschineStammCtrl.SchlossSetzen),
                ["KatalogLoeschen"] = new Func<int, string>(KatalogLoeschen),
                ["KatalogGaben"] = new Func<IReadOnlyDictionary<string, object>>(KaeltemaschineKatalogHuelle.Gaben),
                // KD1 (Anwenderentscheid 13.10.2026): der Weg hinter „Energiekosten..." - die TRAEGERKARTE des
                // Kuehltraegers der Anlage. Er ist PLATTFORMFREI, weil die Energietraegerverwaltung mit ihrer
                // Huelle selbst in EPOS.UI.Daten liegt (dieselbe Gabe wie auf der Kostenseite, KD6a); die Huelle
                // fuellt ihn deshalb selbst - Muster der Naht WaermepumpeAnlageGaben -, Windows und iOS reichen
                // ihn nur durch. Die Komponente engt den Katalog auf STROM ein (EnergietraegerZulaessigkeit),
                // das Geraet ist die Projektkopie der Maschine. Ohne Traeger kein Satz: dann bleibt die
                // benannte Sperre im Dialog stehen.
                ["EnergietraegerGaben"] = new Func<int, int, IReadOnlyDictionary<string, object>>(
                    (traegerId, geraeteId) => traegerId <= 0
                        ? null
                        : new EnergietraegerHuelle(projektId).Gaben(traegerId, DbWerte.ERZEUGER_KAELTEMASCHINE,
                                                                    geraeteId)),
                // Die Kostensummen der Anlage (Kostenkomponente 11) fuer die Zusammenfassung der Detailzeile.
                ["Kostensumme"] = new Func<int, (double Invest, double Betrieb)>(id => Kostensumme(projektId, id)),
                // Die Kenndaten als Bild: Kennlinie (EER, Kaelteleistung) und Teillastkurve - je Projektkopie
                // (katalogsatz = false) oder Stammsatz (true), dieselben Modelle wie in der Verwaltung.
                // K-F5b: die Gruppe „Rückkühlung“ - Katalogsätze samt Lesezeilen und die Luftfeuchte des Projektklimas.
                ["Rueckkuehlung"] = new Func<RueckkuehlungGaben>(() => Rueckkuehlung(projektId)),
                ["Kennlinienbilder"] = new Func<int, bool, KaeltemaschineKennlinienbilder>(
                    (id, katalog) => Satz(id, katalog) is KaeltemaschineModel m
                        ? KaeltemaschineKennlinienbild.Modelle(KaeltemaschineKatalogHuelle.AlsDaten(m)) : null),
                ["Teillastbild"] = new Func<int, bool, WindowsFormsApplication1.Zeichnung.Zeichenmodell>(
                    (id, katalog) => Satz(id, katalog) is KaeltemaschineModel m ? KaeltemaschineTeillastbild.Modell(m) : null)
            };
        }

        /// <summary>Die Anlagen des Projekts samt Kennwerten ihrer Projektkopie.</summary>
        /// <summary>Ein Satz des Katalogs (<paramref name="katalog"/>) oder eine Projektkopie; <c>null</c> = es gibt ihn nicht.</summary>
        /// <summary>
        /// KM-K1: die Projektwärmepumpen im Kühlbetrieb als Zeilen des Dialogs — Kennwerte am Kühlvorlauf, Rang, Kühlträger
        /// und die Zeile „Zulässigkeit“ (Kältemittel der Kopie, Inbetriebnahmejahr ihrer ersten Anlage).
        /// </summary>
        internal static IReadOnlyList<KuehlWaermepumpeZeile> Waermepumpen(int projektId)
            => KuehlerzeugerKatalogCtrl.ProjektWaermepumpen(projektId).Select(w => new KuehlWaermepumpeZeile(
                w.IdWp, w.IdStamm, w.Bezeichner ?? "", w.Firma ?? "", w.Bauart ?? "", w.AnlagenIds.Count, w.Rang,
                w.Kennwert?.KuehlleistungKw, w.Kennwert?.Eer, w.Kennwert?.KaltwasserC, w.Kuehltraeger ?? "", w.EigenerZaehler,
                Zulaessigkeit(projektId, w))).ToList();

        private static string Zulaessigkeit(int projektId, KuehlerzeugerKatalogCtrl.ProjektWaermepumpe w)
        {
            if (w.AnlagenIds.Count == 0) return "";
            try
            {
                object code = DataRepository.ExecuteScalar("SELECT Kaeltemittel FROM Tab_WP WHERE ID = ?", new DbParam("@id", w.IdWp));
                return KaeltemittelZulaessigkeitszeile.Anlage(projektId, w.AnlagenIds[0], true, code as string ?? "") ?? "";
            }
            catch { return ""; }
        }

        /// <summary>KM-K1: übernimmt eine Katalog-Wärmepumpe (Vorbelegung der Einbindung wie im Wärmepumpendialog).</summary>
        internal static KuehlWpUebernahme WaermepumpeUebernehmen(int projektId, int stammId)
        {
            KuehlerzeugerKatalogCtrl.Uebernahme u = KuehlerzeugerKatalogCtrl.WaermepumpeUebernehmen(projektId, stammId, BivalenzAbbildung.Vorbelegen);
            bool angelegt = u.KopieNeu || u.NeueAnlagen.Count > 0;
            return new KuehlWpUebernahme(u.Ok, u.IdWp, u.Bezeichner ?? "", u.SchonImProjekt, u.Meldung, angelegt ? u : null,
                                         u.Hinweis);
        }

        /// <summary>KM-K1: löscht einen Katalogsatz der Wärmepumpe (Katalogweg des Wärmepumpendialogs); "" = gelöscht.</summary>
        internal static string WpKatalogLoeschen(int id)
        {
            WPStammCtrl.KatalogsatzLoeschung l = WPStammCtrl.KatalogsatzLoeschen(id);
            if (!l.Ok) return MyResource.Resource.KMA_MSG_KATALOG_LOESCHFEHLER;
            return "";
        }

        internal static KaeltemaschineModel Satz(int id, bool katalog)
        {
            if (id <= 0) return null;
            return katalog ? KaeltemaschineStammCtrl.Laden(id) : KaeltemaschineCtrl.Laden(id);
        }

        /// <summary>Löscht einen Katalogsatz; leer = gelöscht, sonst der Grund des Kerns.</summary>
        internal static string KatalogLoeschen(int id)
        {
            KaeltemaschineStammCtrl.SpeicherErgebnis e = KaeltemaschineStammCtrl.Loeschen(id);
            if (e.Ok) return "";
            return string.IsNullOrEmpty(e.Meldung) ? MyResource.Resource.KMA_MSG_KATALOG_LOESCHFEHLER : e.Meldung;
        }

        /// <summary>Investition und Betrieb der Anlage (Kostenkomponente 11, <c>KostenSummenCtrl.AnlagenSumme</c>).</summary>
        internal static (double Invest, double Betrieb) Kostensumme(int projektId, int anlagenId)
        {
            if (projektId <= 0 || anlagenId <= 0) return (0, 0);
            return (KostenSummenCtrl.AnlagenSumme(projektId, DbWerte.KOSTEN_KATEGORIE_INVESTITION, anlagenId),
                    KostenSummenCtrl.AnlagenSumme(projektId, DbWerte.KOSTEN_KATEGORIE_BETRIEB, anlagenId));
        }

        internal static IReadOnlyList<KaeltemaschineAnlageDaten> Liste(int projektId)
        {
            List<KaeltemaschineAnlageDaten> liste = KaeltemaschineAnlageCtrl.Liste(projektId).Select(AlsDaten).ToList();
            // Vorauswahl wie bei den übrigen Anlagen (EnergietraegerZulaessigkeit.Vorauswahl): Ohne Kühlträger steht
            // der Stromträger des Projekts in der Wahl. Gespeichert wird er nicht als Abweichung (AlsModell).
            int vor = 0;
            foreach (KaeltemaschineAnlageDaten d in liste)
            {
                if (d.KuehlCarrierId is int id && id > 0) continue;
                if (vor == 0) vor = Vorauswahl(projektId);
                if (vor > 0) d.KuehlCarrierId = vor;
            }
            return liste;
        }

        /// <summary>Die Vorauswahl des Kühlträgers; 0 = keine.</summary>
        internal static int Vorauswahl(int projektId)
        {
            try { return EnergietraegerZulaessigkeit.Vorauswahl(DbWerte.ERZEUGER_KAELTEMASCHINE, 0, projektId); }
            catch { return 0; }
        }

        /// <summary>Die Prüfregeln des Kerns (<see cref="KaeltemaschineAnlageCtrl.Pruefen"/>).</summary>
        internal static string Pruefen(KaeltemaschineAnlageDaten d)
        {
            string grund = KaeltemaschineAnlageCtrl.Pruefen(d == null ? null : AlsModell(d, null));
            if (!string.IsNullOrEmpty(grund) || d?.RueckkuehlwerkWahl is not int) return grund;
            // K-F5b: dieselbe Verträglichkeit wie der Schreibweg - eine luftgekühlte Maschine bekommt kein Rückkühlwerk.
            return d.Geraet is { Luftgekuehlt: true } ? MyResource.Resource.RKW_MSG_LUFT_UNVERTRAEGLICH : null;
        }

        /// <summary>
        /// K-F5c: die Sitzung eines Dialogs „Kältemaschinen im Projekt“ (je Aufruf von <see cref="Gaben"/> eine) — je Anlage
        /// der Stand des Rückkühlwerks vor dem ersten Schreiben der Sitzung samt der Wahl, die ihm entspricht. Abbrechen
        /// schreibt den Stand beim Öffnen über <see cref="Speichern(KaeltemaschineAnlageDaten, Rueckkuehlwerksitzung)"/>
        /// zurück; mit dieser Wahl stellt die Hülle die Projektkopie exakt wieder her.
        /// </summary>
        internal sealed class Rueckkuehlwerksitzung
        {
            internal readonly Dictionary<int, (int? Wahl, KaeltemaschineAnlageCtrl.RueckkuehlwerkStand Stand)> Staende = new();
        }

        /// <summary>Schreibt eine gespeicherte Anlage ohne Dialogsitzung; liefert den Prüftext oder <c>null</c>.</summary>
        internal static string Speichern(KaeltemaschineAnlageDaten d) => Speichern(d, null);

        /// <summary>
        /// Schreibt eine gespeicherte Anlage; liefert den Prüftext oder <c>null</c>. Mit <paramref name="sitzung"/> hält das
        /// erste Schreiben einer Anlage den Stand ihres Rückkühlwerks fest (K-F5c).
        /// </summary>
        internal static string Speichern(KaeltemaschineAnlageDaten d, Rueckkuehlwerksitzung sitzung)
        {
            KaeltemaschineAnlageModel gelesen = d == null ? null : KaeltemaschineAnlageCtrl.Laden(d.AnlagenId);
            if (gelesen == null) return MyResource.Resource.KM_ANLAGE_FEHLT;
            (int? Wahl, KaeltemaschineAnlageCtrl.RueckkuehlwerkStand Stand) start = default;
            if (sitzung != null && !sitzung.Staende.TryGetValue(gelesen.AnlagenId, out start))
            {
                start = (GelesenWahl(gelesen), KaeltemaschineAnlageCtrl.RueckkuehlwerkSchnappschuss(gelesen.AnlagenId));
                sitzung.Staende[gelesen.AnlagenId] = start;
            }
            string grund = KaeltemaschineAnlageCtrl.Speichern(AlsModell(d, gelesen));
            if (!string.IsNullOrEmpty(grund)) return grund;
            return RueckkuehlwerkSchreiben(d, gelesen, start.Stand, start.Wahl);
        }

        /// <summary>
        /// K-F5b: der Schreibschritt der Wahl des Rückkühlwerks im OK-Weg — nur, wenn die Wahl von der gespeicherten
        /// abweicht (ein zweites OK wiederholt nichts, die Projektkopie bleibt mit ihren Werten stehen). Der Kernweg
        /// <see cref="KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen"/> legt die Projektkopie an bzw. löst sie.
        /// K-F5c: Ist die Wahl wieder die des festgehaltenen Stands (<paramref name="start"/>, Abbrechen nach einer
        /// Zwischenspeicherung oder Rückkehr zur Wahl beim Öffnen), die Anlage führt aber eine andere Kopie, schreibt
        /// <see cref="KaeltemaschineAnlageCtrl.RueckkuehlwerkWiederherstellen"/> den Stand exakt zurück — dieselbe Kopie
        /// mit allen Feldern, auch ohne Katalogsatz, oder „keins“.
        /// </summary>
        internal static string RueckkuehlwerkSchreiben(KaeltemaschineAnlageDaten d, KaeltemaschineAnlageModel gelesen,
                                                       KaeltemaschineAnlageCtrl.RueckkuehlwerkStand start = null, int? startWahl = null)
        {
            if (d == null || gelesen == null) return null;
            if (start != null && d.RueckkuehlwerkWahl == startWahl && gelesen.IdRueckkuehlwerk != start.KopieId)
                return KaeltemaschineAnlageCtrl.RueckkuehlwerkWiederherstellen(start);
            if (d.RueckkuehlwerkWahl == KaeltemaschineAnlageDaten.RUECKKUEHLWERK_PROJEKTKOPIE && gelesen.IdRueckkuehlwerk.HasValue)
                return null;
            if (d.RueckkuehlwerkWahl == GelesenWahl(gelesen)) return null;
            int? stamm = d.RueckkuehlwerkWahl is int id && id > 0 ? id : (int?)null;
            return KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(gelesen.AnlagenId, stamm);
        }

        /// <summary>Die Wahl, die der gespeicherten Projektkopie entspricht: ihr Katalogsatz oder die Marke der Kopie.</summary>
        internal static int? GelesenWahl(KaeltemaschineAnlageModel m)
        {
            if (m?.IdRueckkuehlwerk is not int idKopie) return null;
            RueckkuehlwerkModel kopie = RueckkuehlwerkCtrl.LadenStill(idKopie);
            if (kopie == null) return null;
            return kopie.IdStamm is int stamm && stamm > 0 && RueckkuehlwerkStammCtrl.Laden(stamm) != null
                ? stamm : KaeltemaschineAnlageDaten.RUECKKUEHLWERK_PROJEKTKOPIE;
        }

        /// <summary>Modell → Feldsatz samt Kennwerten der Projektkopie.</summary>
        internal static KaeltemaschineAnlageDaten AlsDaten(KaeltemaschineAnlageModel m)
        {
            if (m == null) return null;
            return new KaeltemaschineAnlageDaten
            {
                AnlagenId = m.AnlagenId,
                GeraetId = m.IdKaeltemaschine,
                Bezeichner = m.Bezeichner ?? "",
                Anzahl = m.Anzahl,
                KuehlVorlauf = m.KuehlVorlauf,
                KuehlHilfsstromanteil = m.KuehlHilfsstromanteil,
                KuehlCarrierId = m.KuehlIdCarrier,
                KuehlEigenerZaehler = m.KuehlEigenerZaehler == true,
                Geraet = m.IdKaeltemaschine.HasValue ? Werte(KaeltemaschineCtrl.Laden(m.IdKaeltemaschine.Value)) : null,
                // KM4-E3-b: die Lesezeile „Zulässigkeit“ (Konzept Kaeltemittel als Stammdatum 7.2).
                Zulaessigkeit = m.IdProjekt > 0 && m.AnlagenId > 0
                    ? KaeltemittelZulaessigkeitszeile.Anlage(m.IdProjekt, m.AnlagenId)
                    : "",
                // K-F5b: die Wahl und die Lesezeilen der gespeicherten Projektkopie des Rückkühlwerks.
                RueckkuehlwerkWahl = GelesenWahl(m),
                Rueckkuehlwerk = m.IdRueckkuehlwerk is int idRkw ? Lesung(RueckkuehlwerkCtrl.LadenStill(idRkw)) : null
            };
        }

        /// <summary>
        /// Feldsatz → Modell; Kennung, Projekt und Gerät kommen aus dem gelesenen Stand, sonst aus dem Feldsatz.
        /// Ein leeres Anzahlfeld wird 0 — die Prüfung lehnt es ab.
        /// </summary>
        internal static KaeltemaschineAnlageModel AlsModell(KaeltemaschineAnlageDaten d, KaeltemaschineAnlageModel gelesen)
        {
            return new KaeltemaschineAnlageModel
            {
                AnlagenId = gelesen?.AnlagenId ?? d.AnlagenId,
                IdProjekt = gelesen?.IdProjekt ?? 0,
                IdKaeltemaschine = gelesen != null ? gelesen.IdKaeltemaschine : d.GeraetId,
                Bezeichner = d.Bezeichner ?? "",
                Anzahl = d.Anzahl ?? 0,
                KuehlVorlauf = d.KuehlVorlauf,
                KuehlHilfsstromanteil = d.KuehlHilfsstromanteil,
                KuehlIdCarrier = Kuehltraeger(d, gelesen),
                KuehlEigenerZaehler = d.KuehlEigenerZaehler ? true : (bool?)null
            };
        }

        /// <summary>
        /// Der zu schreibende Kühlträger: Stand vorher keiner und ist die Wahl der Stromträger des Projekts (die
        /// Vorauswahl), bleibt die Spalte leer — leer heißt „wie Heizbetrieb“ und folgt dem Projekt
        /// (<see cref="Kaeltestromabrechnung.Abweichend"/>). Jede andere Wahl wird geschrieben.
        /// </summary>
        private static int? Kuehltraeger(KaeltemaschineAnlageDaten d, KaeltemaschineAnlageModel gelesen)
        {
            if (gelesen != null && !(gelesen.KuehlIdCarrier is int alt && alt > 0)
                && d.KuehlCarrierId is int neu && gelesen.IdProjekt > 0
                && neu == Kaeltestromabrechnung.Projekttraeger(gelesen.IdProjekt))
                return gelesen.KuehlIdCarrier;
            return d.KuehlCarrierId;
        }

        /// <summary>Die Kennwerte eines Geräts für die Anzeige; <c>null</c> bleibt <c>null</c>.</summary>
        // =================================================================== K-F5b: Rückkühlung

        /// <summary>
        /// Die Gaben der Gruppe „Rückkühlung“: alle Katalogsätze der Rückkühlwerke samt Lesezeilen und ob das Klima des
        /// Projekts eine Luftfeuchte führt (<see cref="KaeltemaschineAnlageCtrl.KlimaMitFeuchte"/>). Vor dem Schemaschritt
        /// <c>null</c> — dann keine Gruppe.
        /// </summary>
        internal static RueckkuehlungGaben Rueckkuehlung(int projektId)
        {
            if (!RueckkuehlwerkSchema.AnlagenspaltenVorhanden()) return null;
            var katalog = RueckkuehlwerkStammCtrl.Liste()
                .Select(z => RueckkuehlwerkStammCtrl.Laden(z.Id))
                .Where(m => m != null)
                .Select(m => new RueckkuehlwerkEintrag(m.Id, Eintragstext(m), Lesung(m, m.Id)))
                .ToList();
            bool feuchte;
            try { feuchte = KaeltemaschineAnlageCtrl.KlimaMitFeuchte(projektId); }
            catch { feuchte = false; }
            return new RueckkuehlungGaben { Katalog = katalog, KlimaMitFeuchte = feuchte };
        }

        private static string Eintragstext(RueckkuehlwerkModel m)
            => (m.Bezeichner ?? "") + " · " + RueckkuehlwerkStammCtrl.BauartText(m.Bauart);

        /// <summary>
        /// Die Lesezeilen eines Rückkühlwerks — Bauart, Nennleistung, Annäherung samt Weg, Ventilator samt Regelung,
        /// Schaltung der freien Kühlung; ein leeres Feld zeigt „Vorgabe: …“ mit dem Wert des Rechenwegs
        /// (<see cref="Rueckkuehlwerk.AusModell"/>), wie die Verwaltung. <c>null</c> bleibt <c>null</c>.
        /// </summary>
        internal static RueckkuehlwerkLesung Lesung(RueckkuehlwerkModel m, int? stammId = null)
        {
            if (m == null) return null;
            var kultur = System.Globalization.CultureInfo.CurrentCulture;
            Rueckkuehlwerk r = Rueckkuehlwerk.AusModell(m);
            Func<string, string> vorgabe = t => string.Format(kultur, MyResource.Resource.RKW_VORGABE, t);
            Func<double, string, string> zahl = (w, f) => w.ToString(f, kultur);

            string bauart = string.IsNullOrEmpty(m.Bauart) ? vorgabe(RueckkuehlwerkStammCtrl.BauartText(null))
                                                         : RueckkuehlwerkStammCtrl.BauartText(m.Bauart);
            string nenn = m.Nennleistung_kW is double kw ? zahl(kw, "0.#") + " kW" : "–";
            string annaeherung = (m.Annaeherung_Nenn_K is double k ? zahl(k, "0.#") + " K" : vorgabe(zahl(r.AnnaeherungK, "0.#") + " K"))
                                 + " · " + (string.IsNullOrEmpty(m.Annaeherung_Weg) ? vorgabe(RueckkuehlwerkStammCtrl.WegText(null))
                                                                                     : RueckkuehlwerkStammCtrl.WegText(m.Annaeherung_Weg));
            string regelung = string.IsNullOrEmpty(m.Ventilator_Regelung) ? vorgabe(RueckkuehlwerkStammCtrl.RegelungText(null))
                                                                          : RueckkuehlwerkStammCtrl.RegelungText(m.Ventilator_Regelung);
            string ventilator = m.Ventilator_Nenn_kW is double v
                ? zahl(v, "0.##") + " kW · " + regelung
                : MyResource.Resource.RKW_VORGABE_VENTILATOR;
            string schaltung = string.IsNullOrEmpty(m.Freikuehlung_Schaltung) ? vorgabe(RueckkuehlwerkStammCtrl.SchaltungText(null))
                                                                              : RueckkuehlwerkStammCtrl.SchaltungText(m.Freikuehlung_Schaltung);
            return new RueckkuehlwerkLesung
            {
                Bezeichner = m.Bezeichner ?? "",
                StammId = stammId ?? (m.IdStamm is int s && s > 0 ? s : (int?)null),
                Zeilen = new List<(string, string)>
                {
                    (MyResource.Resource.RKW_LBL_BAUART, bauart),
                    (MyResource.Resource.RKW_LBL_NENNLEISTUNG, nenn),
                    (MyResource.Resource.RKW_GRUPPE_ANNAEHERUNG, annaeherung),
                    (MyResource.Resource.RKW_GRUPPE_VENTILATOR, ventilator),
                    (MyResource.Resource.RKW_GRUPPE_FREIKUEHLUNG, schaltung),
                },
                Verdunstungskuehlanlage = r.Verdunstungskuehlanlage,
                VentilatorKw = m.Ventilator_Nenn_kW
            };
        }

        internal static KaeltemaschineGeraetwerte Werte(KaeltemaschineModel m)
        {
            if (m == null) return null;
            // KM3-E3-b: die Lesewerte der Teillastrechnung - dieselbe Herleitung wie der Lauf (Kaeltemaschinenteillast).
            KaeltemaschineTeillastLesewerte t = KaeltemaschineTeillastLesewerte.Aus(m);
            var kultur = System.Globalization.CultureInfo.CurrentUICulture;
            return new KaeltemaschineGeraetwerte
            {
                TeillastWeg = t.WegText(kultur),
                Verdichterregelung = t.RegelungText(kultur),
                Taktverlustfaktor = t.CdText(System.Globalization.CultureInfo.CurrentCulture),
                Kennfeldrand = t.RandwegText(kultur),
                TeillastGesetzt = t.Gesetzt,
                Bezeichner = m.Bezeichner ?? "",
                Firma = m.Firma ?? "",
                Typ = m.Typ ?? "",
                Rueckkuehlart = m.Rueckkuehlart == null ? "" : KaeltemaschineStammCtrl.RueckkuehlartText(m.Rueckkuehlart),
                Nennkaelteleistung = m.Nennkaelteleistung_kW,
                NennEer = m.Nenn_EER,
                Mindestteillast = m.Mindestteillast_Prozent,
                KaltwasserMin = m.Kaltwasser_Vorlauf_Min,
                Saisonkennzahl = m.Saisonkennzahl,
                SaisonkennzahlArt = m.Saisonkennzahl_Art ?? "",
                Geraeteart = string.IsNullOrEmpty(m.Geraeteart) ? "" : KaeltemaschineStammCtrl.GeraeteartText(m.Geraeteart),
                Kaeltemittel = m.Kaeltemittel ?? "",
                Gwp = m.Kaeltemittel_GWP,
                Geraetekosten = m.Geraetekosten,
                // Die eine Regel des Kerns - wie der Lauf rechnet eine leere Rückkühlart luftgekühlt.
                Luftgekuehlt = KaeltemaschineSchema.IstLuftgekuehlt(m.Rueckkuehlart),
                Kaltwasserstuetzstellen = (m.Kennlinie ?? new List<KaeltemaschineKenndatenModel>())
                    .Select(k => k.Kaltwassertemperatur).Distinct().OrderBy(t => t).ToList()
            };
        }
    }
}
