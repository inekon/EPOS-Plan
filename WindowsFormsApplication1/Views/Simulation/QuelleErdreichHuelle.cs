using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Simulation;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>QuelleErdreichDialog</c> (iU9-W10a.3) — der Ersatz für
    /// <c>Form_QuelleErdreich</c>.
    ///
    /// <para><b>Zwei Delegaten, kein Datenzugriff sonst.</b> Die Fachrechnung des
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
    ///   <item><description><c>Jahresgangbild</c> — das PNG aus
    ///     <c>ChartRenderer.Jahresgang</c>, ebenfalls auf einem eigenen Faden. Der Kern
    ///     zeichnet, die Oberfläche zeigt (Hausregel seit iU7-5).</description></item>
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
        internal static IReadOnlyDictionary<string, object> Gaben(QuelleErdreichDaten daten)
        {
            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Lauf"] = ErdreichAuswertung.ErgebnisZuordnen(
                    ErgebnisDesLaufs(daten)),

                ["Simulieren"] = Simulationslauf(daten),
                ["Jahresgangbild"] = Bildzeichner(),

                ["TitelText"] = MyResource.Resource.SIMQ_ERDREICH_TITEL,
                ["TitelMitWp"] = MyResource.Resource.SIMQ_ERDREICH_TITEL_MIT_WP,
                ["GbQuellsystem"] = MyResource.Resource.SIMQ_ERDREICH_GB_QUELLSYSTEM,
                ["GbStandort"] = MyResource.Resource.SIMQ_ERDREICH_GB_STANDORT,
                ["GbVorschau"] = MyResource.Resource.SIMQ_ERDREICH_GB_VORSCHAU,
                ["GbPruefung"] = MyResource.Resource.SIMQ_ERDREICH_GB_PRUEFUNG,
                ["RbKollektor"] = MyResource.Resource.SIMQ_ERDREICH_RB_KOLLEKTOR,
                ["RbSonde"] = MyResource.Resource.SIMQ_ERDREICH_RB_SONDE,
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
                ["SimNurGespeichert"] = MyResource.Resource.SIMQ_ERDREICH_SIM_NUR_GESPEICHERT,

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
        /// Der Delegat <c>Simulieren</c>: rechnet das Projekt durch und ordnet der
        /// Anlage ihr Ergebnis zu. Der LAUF läuft auf einem eigenen Faden.
        /// </summary>
        private static Func<int, Task<(ErdreichAuswertung.ErdreichLaufErgebnis, string)>>
            Simulationslauf(QuelleErdreichDaten daten)
        {
            return idProjekt => Task.Run(() =>
            {
                string fehler;
                bool ok = new SimulationRunner().Simuliere(idProjekt, out fehler);
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
                    ErdreichAuswertung.ErgebnisZuordnen(ErgebnisDesLaufs(daten));
                return (erg.Vorhanden ? erg : null, (string)null);
            });
        }

        /// <summary>
        /// Der Delegat <c>Jahresgangbild</c>: zwei Stundenreihen hinein, ein PNG heraus.
        /// Die Außentemperatur darf fehlen — dann zeichnet der Renderer eine Reihe.
        /// </summary>
        private static Func<double[], double[], Task<byte[]>> Bildzeichner()
        {
            return (quelle, aussen) => Task.Run(() =>
            {
                var reihen = new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe(
                        MyResource.Resource.CHART_SERIE_QUELLTEMPERATUR, quelle,
                        ChartRenderer.C_QUELLTEMPERATUR)
                };
                if (aussen != null && aussen.Length > 1)
                    reihen.Add(new ChartRenderer.Reihe(
                        MyResource.Resource.CHART_SERIE_AUSSENTEMPERATUR, aussen,
                        ChartRenderer.C_AUSSENTEMPERATUR));

                return ChartRenderer.Jahresgang(
                    MyResource.Resource.SIMQ_ERDREICH_GB_VORSCHAU, reihen,
                    MyResource.Resource.CHART_ACHSE_MONAT,
                    MyResource.Resource.CHART_ACHSE_QUELLTEMPERATUR);
            });
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
}
