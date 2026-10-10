using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Simulation;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>QuelleErdreichDialog</c> (iU9-W10a.3) — der Ersatz für
    /// <c>Form_QuelleErdreich</c>.
    ///
    /// <para><b>Nur Delegaten, kein Datenzugriff sonst.</b> Die Fachrechnung des
    /// Dialogs — Bodenkennwerte, Jahresprofile, VDI-4640-Prüfung — steht in
    /// <c>ErdreichTemperatur</c>, <c>VDI4640Pruefung</c> und <c>ErdreichAuswertung</c>
    /// und braucht keine Datenbank; die Komponente ruft sie direkt. Die Hülle liefert
    /// nur, was sie NICHT kann:</para>
    /// <list type="bullet">
    ///   <item><description><c>Simulieren</c> — der vollständige Lauf, auf einem
    ///     EIGENEN FADEN (Befund W10-B9, Abweichung A-5). Probe R-W10a-2 hat gezeigt,
    ///     dass <c>SimulationRunner.Simuliere</c> dort fehlerfrei läuft: Der
    ///     Datenzugriff öffnet je Aufruf eine eigene Verbindung und hält nichts am
    ///     Faden fest.</description></item>
    ///   <item><description><c>Jahresgangmodell</c> — das Zeichenmodell aus
    ///     <c>ChartRenderer.JahresgangModell</c>, ebenfalls auf einem eigenen Faden. Der
    ///     Kern zeichnet, die Oberfläche zeigt (Hausregel seit iU7-5).</description></item>
    ///   <item><description><c>FarbeSetzen</c> / <c>FarbeZuruecksetzen</c> — der Klick
    ///     auf das Farbfeld eines Legendeneintrags am Bild. Die zwei Reihen der
    ///     Vorschau tragen Farbrollen, also gibt es dort etwas zu wählen (Farbrollen,
    ///     Bedienung Teil 2).</description></item>
    /// </list>
    ///
    /// <para><b>Die Dreistufenlogik der Ergebniszuordnung</b> (<c>ErgebnisDesLaufs</c>
    /// :1126-1142) bleibt hier: erst die Anlagen-Id, dann der Modulname, dann „es gibt
    /// nur eines". Sie ist Sache des Aufrufers, nicht des Dialogs — die Komponente
    /// bekommt ein fertiges Ergebnis oder keines.</para>
    /// </summary>
    internal static class QuelleErdreichHuelle
    {
        // iU9-W10b.1: Der FENSTERWEG dieser Huelle ist entfallen. Ihr einziger
        // Aufrufer war Form_Simulation_Config; seit die Simulationskonfiguration
        // selbst eine Razor-Seite ist, erscheint der Dialog als UEBERLAGERUNG in
        // ihrem Fenster (Risiko R2 - nie zwei WebViews uebereinander). Was bleibt,
        // ist der PARAMETERSATZ unten: Er war von Anfang an fuer genau diesen Tag
        // getrennt gehalten (W10a, "Gaben ohne Geschlossen").

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, damit ihn ab W10b
        /// auch die Überlagerung in der Simulationsseite nehmen kann.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(QuelleErdreichDaten daten,
                                                                  ErdreichLaufsitzung sitzung = null)
        {
            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Lauf"] = LaufOderGespeichert(daten),
                ["StandDesLaufs"] = MyResource.Resource.SIMQ_ERDREICH_STAND_LAUF,

                ["Simulieren"] = Simulationslauf(daten, sitzung),
                ["Jahresgangmodell"] = Modellzeichner(),

                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>(FarbeSetzen),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(FarbeZuruecksetzen),

                ["TitelText"] = MyResource.Resource.SIMQ_ERDREICH_TITEL,
                ["TitelMitWp"] = MyResource.Resource.SIMQ_ERDREICH_TITEL_MIT_WP,
                ["GbQuellsystem"] = MyResource.Resource.SIMQ_ERDREICH_GB_QUELLSYSTEM,
                ["GbStandort"] = MyResource.Resource.SIMQ_ERDREICH_GB_STANDORT,
                ["GbVorschau"] = MyResource.Resource.SIMQ_ERDREICH_GB_VORSCHAU,
                ["GbPruefung"] = MyResource.Resource.SIMQ_ERDREICH_GB_PRUEFUNG,
                ["VorpruefungKopf"] = MyResource.Resource.SIMQ_ERDREICH_VORPRUEFUNG_KOPF,
                ["HinweisSondeKonstant"] = MyResource.Resource.SIMQ_ERDREICH_HINWEIS_SONDE_KONSTANT,
                ["HinweisKollektorLauf"] = MyResource.Resource.SIMQ_ERDREICH_HINWEIS_KOLLEKTOR_LAUF,
                ["KennwerteLaufZeile"] = MyResource.Resource.SIMQ_ERDREICH_KENNWERTE_LAUF,
                ["RbKollektor"] = MyResource.Resource.SIMQ_ERDREICH_RB_KOLLEKTOR,
                ["RbSonde"] = MyResource.Resource.SIMQ_ERDREICH_RB_SONDE,
                ["RbKollektorWahl"] = MyResource.Resource.SIMQ_ERDREICH_RB_KOLLEKTOR_WAHL,
                ["RbSondeWahl"] = MyResource.Resource.SIMQ_ERDREICH_RB_SONDE_WAHL,
                ["LblVerlegetiefe"] = MyResource.Resource.SIMQ_ERDREICH_VERLEGETIEFE,
                ["LblFlaeche"] = MyResource.Resource.SIMQ_ERDREICH_FLAECHE,
                ["LblLaengeSonde"] = MyResource.Resource.SIMQ_ERDREICH_LAENGE_SONDE,
                ["LblAnzahlSonden"] = MyResource.Resource.SIMQ_ERDREICH_ANZAHL_SONDEN,
                ["LblBodentyp"] = MyResource.Resource.SIMQ_ERDREICH_BODENTYP,
                ["LblBodentypHinweis"] = MyResource.Resource.SIMQ_ERDREICH_BODENTYP_HINWEIS,
                ["LblKlimazone"] = MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE,
                ["LblKlimazoneHinweis"] = MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE_HINWEIS,
                ["LblSpreizung"] = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG,
                ["LblSpreizungHinweis"] = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG_HINWEIS,
                ["LblSondenabstand"] = MyResource.Resource.SIMQ_ERDREICH_SONDENABSTAND,
                ["LblBohrlochdurchmesser"] = MyResource.Resource.SIMQ_ERDREICH_BOHRLOCHDURCHMESSER,
                ["LblBohrlochwiderstand"] = MyResource.Resource.SIMQ_ERDREICH_BOHRLOCHWIDERSTAND,
                ["LblKopfueberdeckung"] = MyResource.Resource.SIMQ_ERDREICH_KOPFUEBERDECKUNG,
                ["LblBetrachtungsjahr"] = MyResource.Resource.SIMQ_ERDREICH_BETRACHTUNGSJAHR,
                ["LblSondenanordnung"] = MyResource.Resource.SIMQ_ERDREICH_SONDENANORDNUNG,
                ["LblAnordnungQuadratisch"] = MyResource.Resource.SIMQ_ERDREICH_ANORDNUNG_QUADRATISCH,
                ["LblAnordnungReihe"] = MyResource.Resource.SIMQ_ERDREICH_ANORDNUNG_REIHE,
                ["LblVorgabe"] = MyResource.Resource.SIMQ_ERDREICH_VORGABE,
                ["LblSondenfeldHinweis"] = MyResource.Resource.SIMQ_ERDREICH_SONDENFELD_HINWEIS,
                // Der Knopftext ist ein SYMBOL und bleibt unuebersetzt (Katalogregel);
                // was er tut, steht im Kurztext daneben.
                ["BtnKarte"] = "…",
                ["KarteKnopfTip"] = MyResource.Resource.SIMQ_KARTE_KNOPF_TIP,
                ["KarteTitel"] = MyResource.Resource.SIMQ_KARTE_TITEL,
                ["BtnSimulation"] = MyResource.Resource.SIMQ_ERDREICH_BTN_SIMULATION,
                ["OkText"] = MyResource.Resource.SIM_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,
                ["BildAlt"] = MyResource.Resource.SIMQ_ERDREICH_GB_VORSCHAU,
                ["PlatzhalterText"] = MyResource.Resource.SIMQ_ERDREICH_BILD_PLATZHALTER,

                ["ZoneNichtZugeordnet"] = MyResource.Resource.SIMQ_ERDREICH_ZONE_NICHT_ZUGEORDNET,
                ["BodenkennwerteText"] = MyResource.Resource.SIMQ_ERDREICH_BODENKENNWERTE,
                ["OhneKlimadaten"] = MyResource.Resource.SIMQ_ERDREICH_OHNE_KLIMADATEN,
                ["PruefungKeinLauf"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_PRUEFUNG_KEIN_LAUF),
                ["HinweisFestgestein"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_HINWEIS_FESTGESTEIN),
                ["HinweisVorbehalt"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_HINWEIS_VORBEHALT),
                ["AenderungHinweis"] = MyResource.Resource.SIMQ_ERDREICH_AENDERUNG_HINWEIS,

                ["WarteTitel"] = MyResource.Resource.SIMQ_ERDREICH_BTN_SIMULATION,
                ["WarteText"] = MyResource.Resource.SIMQ_ERDREICH_SIM_LAEUFT,

                ["MsgZahlKollektor"] = MyResource.Resource.SIMQ_ERDREICH_MSG_ZAHL_KOLLEKTOR,
                ["MsgTiefeNull"] = MyResource.Resource.SIMQ_ERDREICH_MSG_TIEFE_NULL,
                ["MsgTiefeMax"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_MSG_TIEFE_MAX),
                ["MsgFlaeche"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_MSG_FLAECHE),
                ["MsgZahlSonde"] = MyResource.Resource.SIMQ_ERDREICH_MSG_ZAHL_SONDE,
                ["MsgLaengeNull"] = MyResource.Resource.SIMQ_ERDREICH_MSG_LAENGE_NULL,
                ["MsgAnzahlMin"] = MyResource.Resource.SIMQ_ERDREICH_MSG_ANZAHL_MIN,
                ["MsgSondenfeld"] = MyResource.Resource.SIMQ_ERDREICH_MSG_SONDENFELD,
                ["MsgSpreizung"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_MSG_SPREIZUNG),

                ["MsgSimOhneProjekt"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_OHNE_PROJEKT),
                ["MsgSimFehler"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_FEHLER),
                ["MsgSimOhneErgebnis"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_OHNE_ERGEBNIS),

                ["HilfeSchluessel"] = "Form_QuelleErdreich.btn_Help"
            };
        }

        /// <summary>
        /// Die AUSLEGUNGSWERTE der Wärmepumpen dieser Anlage für die Vorprüfung ohne Lauf:
        /// Heizleistung und COP am Normpunkt der Kennlinie der Projektkopie, gelesen im
        /// Kern (<c>ErdreichVorpruefungCtrl</c>). Ein Lesefehler ergibt eine leere Liste —
        /// der Dialog nennt dann den fehlenden Wert.
        /// </summary>
        internal static IReadOnlyList<WpAuslegung> Auslegung(int idProjekt, int idAnlage)
        {
            var liste = new List<WpAuslegung>();
            foreach (VDI4640Pruefung.Auslegungswert a in
                     ErdreichVorpruefungCtrl.Auslegungswerte(idProjekt, idAnlage))
                liste.Add(new WpAuslegung(a.Modul, a.NennheizleistungKw, a.Cop, a.Normpunkt, a.LuftWasser));
            return liste;
        }

        /// <summary>
        /// Der Delegat <c>Simulieren</c>: rechnet das Projekt durch und ordnet der
        /// Anlage ihr Ergebnis zu. Der LAUF läuft auf einem eigenen Faden.
        ///
        /// <para><b>Gerechnet wird mit den ANGEZEIGTEN Eingaben</b> (Anwendermeldung 10.10.2026):
        /// Der Dialog reicht seinen geprüften Eingabesatz herein, und die Hülle legt ihn als
        /// <see cref="ErdreichLaufvorgabe"/> über die gespeicherten Werte der Anlage — gelesen, nicht
        /// geschrieben; geschrieben wird erst im OK-Weg. Ohne Satz rechnet der Lauf mit dem Stand beim
        /// Öffnen.</para>
        /// </summary>
        private static Func<QuelleErdreichDaten, Task<(ErdreichAuswertung.ErdreichLaufErgebnis, string)>>
            Simulationslauf(QuelleErdreichDaten daten, ErdreichLaufsitzung sitzung)
        {
            return eingaben => SpeicherEngine.Kulturweitergabe.Starten(() =>
            {
                QuelleErdreichDaten satz = eingaben ?? daten;
                sitzung?.VorDemLauf();
                using (Laufvorgabe(satz).Anwenden())
                try
                {
                    string fehler;
                    bool ok = new SimulationRunner().Simuliere(satz.IdProjekt, out fehler);
                    if (!ok)
                    {
                        // Ein Lauf ohne Fehlertext ist kein stiller Erfolg - der Dialog
                        // braucht etwas zu sagen.
                        return (null,
                                string.IsNullOrEmpty(fehler)
                                    ? MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_OHNE_ERGEBNIS
                                    : fehler);
                    }

                    ErdreichAuswertung.ErdreichLaufErgebnis erg =
                        ErdreichAuswertung.ErgebnisZuordnen(ErgebnisDesLaufs(satz));
                    return (erg.Vorhanden ? erg : null, (string)null);
                }
                finally
                {
                    sitzung?.NachDemLauf(satz);
                }
            });
        }

        /// <summary>
        /// Der Schlüssel eines Eingabesatzes, wie ihn der OK-Weg schreibt: Quelle, Sondenfeld und
        /// Klimazone. Zwei Sätze mit gleichem Schlüssel rechnen denselben Lauf.
        /// </summary>
        internal static string Eingabeschluessel(QuelleErdreichDaten e)
        {
            if (e == null) return "";
            QuelleErgebnis q = Quelle(e);
            ErdsondenfeldEingabe f = Sondenfeld(e);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            return string.Join("|",
                (q.Quellsystem ?? "").ToUpperInvariant(), q.Tiefe.ToString("R", ci), q.Flaeche.ToString("R", ci),
                q.Anzahl.ToString(ci), q.Bodentyp ?? "", q.SpreizungErdreich.ToString("R", ci),
                f?.AbstandM?.ToString("R", ci), f?.BohrlochdurchmesserMm?.ToString("R", ci),
                f?.Bohrlochwiderstand?.ToString("R", ci), f?.KopfueberdeckungM?.ToString("R", ci),
                f?.Betrachtungsjahr?.ToString(ci), f?.Anordnung?.ToString(), e.Klimazone.ToString(ci));
        }

        /// <summary>
        /// Die Wärmequelle des Eingabesatzes, wie der OK-Weg sie schreibt
        /// (<see cref="WaermequelleClass.QuelleSchreiben"/>, Typ Erdreich) — EINE Abbildung für
        /// Schreiben und Lauf.
        /// </summary>
        internal static QuelleErgebnis Quelle(QuelleErdreichDaten e) => new QuelleErgebnis
        {
            Typ = WaermequelleClass.TYP_ERDREICH,
            Quellsystem = e.Quellsystem,
            Tiefe = e.Tiefe,
            Flaeche = e.Flaeche,
            Anzahl = e.Anzahl,
            Bodentyp = e.Bodentyp,
            SpreizungErdreich = e.Spreizung
        };

        /// <summary>
        /// Das Sondenfeld des Eingabesatzes (Konzept 23.3) — nur beim Quellsystem Sonde, sonst
        /// <c>null</c>; leer heißt Vorgabe.
        /// </summary>
        internal static ErdsondenfeldEingabe Sondenfeld(QuelleErdreichDaten e)
            => string.Equals(e.Quellsystem, ErdreichTemperatur.QUELLSYSTEM_SONDE, StringComparison.OrdinalIgnoreCase)
                ? new ErdsondenfeldEingabe
                {
                    AbstandM = e.Sondenabstand,
                    BohrlochdurchmesserMm = e.Bohrlochdurchmesser,
                    Bohrlochwiderstand = e.Bohrlochwiderstand,
                    KopfueberdeckungM = e.Kopfueberdeckung,
                    Betrachtungsjahr = e.Betrachtungsjahr,
                    Anordnung = ErdsondenfeldCtrl.AnordnungAusText(e.Sondenanordnung)
                }
                : null;

        /// <summary>Der Eingabesatz als Vorgabe des Laufs (<see cref="ErdreichLaufvorgabe"/>).</summary>
        internal static ErdreichLaufvorgabe Laufvorgabe(QuelleErdreichDaten e)
            => ErdreichLaufvorgabe.Aus(e.IdProjekt, e.IdAnlage, Quelle(e), Sondenfeld(e), e.Klimazone);

        /// <summary>
        /// Der Delegat <c>Jahresgangmodell</c>: zwei Stundenreihen (dazu nach einem Lauf die gerechnete) hinein, ein
        /// ZEICHENMODELL heraus. Die Außentemperatur darf fehlen — dann zeichnet der
        /// Renderer eine Reihe.
        ///
        /// <para><b>Seit der Etappe DG-E3 ist es kein PNG mehr</b>
        /// (<c>JahresgangModell</c> statt <c>Jahresgang</c>): Das Bild steht im
        /// Baustein <c>DiagrammSvg</c>, der Zeitausschnitt ist die <c>viewBox</c>
        /// seiner Zeichenfläche. Gerechnet wird weiterhin auf einem EIGENEN FADEN —
        /// 8 760 Stützstellen je Reihe bleiben 8 760, ob sie in ein Pixelbild oder in
        /// einen Knotenbaum münden.</para>
        ///
        /// <para><b>Der Dialog hält das Ergebnis</b> (sein Feld <c>_modell</c>) und
        /// fordert nur nach einer Eingabe ein neues an; das ist dieselbe
        /// Zwischenspeicherung, die er für die Bytes führte, und zugleich die
        /// Bedingung des Bausteins: Er baut seinen Knotenbaum nur neu, wenn die
        /// REFERENZ des Modells wechselt.</para>
        /// </summary>
        private static Func<double[], double[], double[], Task<Zeichenmodell>> Modellzeichner()
        {
            return (quelle, aussen, gerechnet) => SpeicherEngine.Kulturweitergabe.Starten(() =>
            {
                // Nach einem Lauf (Anwenderwunsch 08.10.2026) heisst die Auslegungsreihe „ungestört",
                // daneben steht die gerechnete Soletemperatur des letzten Laufs. Ohne Lauf bleibt das
                // Bild, wie es war.
                bool mitLauf = gerechnet != null && gerechnet.Length > 1;
                var reihen = new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe(
                        mitLauf ? MyResource.Resource.CHART_SERIE_QUELLTEMPERATUR_UNGESTOERT
                                : MyResource.Resource.CHART_SERIE_QUELLTEMPERATUR, quelle,
                        Farbrolle.QUELLTEMPERATUR)
                };
                if (mitLauf)
                    reihen.Add(new ChartRenderer.Reihe(
                        MyResource.Resource.CHART_SERIE_QUELLTEMPERATUR_GERECHNET, gerechnet,
                        Farbrolle.SERIE_2));
                if (aussen != null && aussen.Length > 1)
                    reihen.Add(new ChartRenderer.Reihe(
                        MyResource.Resource.CHART_SERIE_AUSSENTEMPERATUR, aussen,
                        Farbrolle.AUSSENTEMPERATUR));

                return ChartRenderer.JahresgangModell(
                    MyResource.Resource.SIMQ_ERDREICH_GB_VORSCHAU, reihen,
                    MyResource.Resource.CHART_ACHSE_MONAT,
                    MyResource.Resource.CHART_ACHSE_QUELLTEMPERATUR);
            });
        }

        // =================================================================
        // Die Farbe einer Reihe (Farbrollen, Bedienung Teil 2)
        // =================================================================

        /// <summary>
        /// Der Klick auf das Farbfeld eines Legendeneintrags: Die Rolle bekommt
        /// anwendungsweit diese Farbe — Bildschirm wie Bericht.
        ///
        /// <para>Die zwei Reihen der Vorschau tragen die Rollen
        /// <c>QUELLTEMPERATUR</c> und <c>AUSSENTEMPERATUR</c>; erst damit hat der
        /// Wähler am Bild etwas zu setzen.</para>
        /// </summary>
        private static Task FarbeSetzen(Farbrolle rolle, Farbe farbe)
        {
            Diagrammfarben.Setze(rolle, farbe);
            return Task.CompletedTask;
        }

        /// <summary>„Hausfarbe": Der Eintrag fällt aus der Einstellung.</summary>
        private static Task FarbeZuruecksetzen(Farbrolle rolle)
        {
            Diagrammfarben.Zuruecksetzen(rolle);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Das Ergebnis, mit dem der Dialog öffnet (EQ1): der Lauf DIESER Sitzung, wenn es einen
        /// gibt — sonst die gespeicherte Prüfung des letzten gespeicherten Laufs
        /// (<c>Tab_ErgebnisErdreich</c>) samt Laufstempel, den der Dialog als „Stand des Laufs
        /// vom …" zeigt.
        /// </summary>
        internal static ErdreichAuswertung.ErdreichLaufErgebnis LaufOderGespeichert(QuelleErdreichDaten daten)
        {
            ErdreichAuswertung.ErdreichLaufErgebnis frisch =
                ErdreichAuswertung.ErgebnisZuordnen(ErgebnisDesLaufs(daten));
            if (frisch.Vorhanden || daten == null || daten.IdProjekt <= 0) return frisch;
            try { return ErdreichErgebnisSpeicher.Gespeichert(daten.IdProjekt, daten.IdAnlage); }
            catch { return frisch; }
        }

        /// <summary>
        /// Das Ergebnis DIESER Anlage aus dem letzten Lauf des Projekts — die drei
        /// Stufen aus <c>ErgebnisDesLaufs</c>:1126-1142: erst die Anlagen-Id, dann der
        /// Modulname, dann „es gibt nur eines".
        /// </summary>
        private static ErdreichAuswertung.AnlageErgebnis ErgebnisDesLaufs(
            QuelleErdreichDaten daten)
        {
            if (daten == null || daten.IdProjekt <= 0) return null;

            ErdreichAuswertung.AnlageErgebnis einziges = null;
            int anzahl = 0;

            foreach (ErdreichAuswertung.AnlageErgebnis a in
                     ErdreichAuswertung.FuerProjekt(daten.IdProjekt))
            {
                if (daten.IdAnlage > 0 && a.ID_Anlage == daten.IdAnlage) return a;
                if (!string.IsNullOrEmpty(daten.WPName) &&
                    string.Equals(a.Modul, daten.WPName, StringComparison.Ordinal)) return a;

                anzahl++;
                einziges = a;
            }

            return anzahl == 1 ? einziges : null;
        }
    }

    /// <summary>
    /// Ein geöffneter Erdreichdialog und seine Läufe (Hausregel „Abbrechen schließt ohne zu
    /// speichern“): Vor dem ersten Lauf sichert sie den Stand des Projekts im Zwischenspeicher
    /// (<see cref="ErdreichAuswertung.StandDesProjekts"/>); endet der Dialog mit Abbrechen, ✕ oder Esc
    /// und hat der letzte Lauf mit einem Eingabesatz gerechnet, der vom gespeicherten abweicht, legt
    /// sie den gesicherten Stand zurück. Ein Lauf mit dem gespeicherten Satz und jeder Lauf vor einem
    /// OK bleibt.
    /// </summary>
    internal sealed class ErdreichLaufsitzung
    {
        private readonly int _idProjekt;
        private readonly string _gespeichert;
        private bool _gesichert;
        private List<ErdreichAuswertung.AnlageErgebnis> _vorher;
        private bool _abweichend;

        /// <param name="gespeichert">Der Satz beim Öffnen — der gespeicherte Stand der Anlage.</param>
        public ErdreichLaufsitzung(QuelleErdreichDaten gespeichert)
        {
            _idProjekt = gespeichert?.IdProjekt ?? 0;
            _gespeichert = QuelleErdreichHuelle.Eingabeschluessel(gespeichert);
        }

        /// <summary>Hat der letzte Lauf mit einem ungespeicherten Satz gerechnet?</summary>
        public bool LaufAbweichend => _abweichend;

        internal void VorDemLauf()
        {
            if (_gesichert) return;
            _vorher = ErdreichAuswertung.StandDesProjekts(_idProjekt);
            _gesichert = true;
        }

        internal void NachDemLauf(QuelleErdreichDaten satz)
            => _abweichend = !string.Equals(QuelleErdreichHuelle.Eingabeschluessel(satz), _gespeichert,
                                            StringComparison.Ordinal);

        /// <summary>Der Dialog endet ohne OK: einen Lauf mit ungespeicherten Eingaben verwerfen.</summary>
        public void Abgebrochen()
        {
            if (!_gesichert || !_abweichend) return;
            ErdreichAuswertung.StandZuruecklegen(_idProjekt, _vorher);
            _abweichend = false;
        }
    }
}
