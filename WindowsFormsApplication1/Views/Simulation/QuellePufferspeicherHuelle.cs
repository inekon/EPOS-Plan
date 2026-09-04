using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Simulation;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>QuellePufferspeicherDialog</c> (iU9-W10a.5) — der
    /// Ersatz für <c>Form_QuellePufferspeicher</c>.
    ///
    /// <para><b>Die Pufferverwaltung ist eine ÜBERLAGERUNG, kein zweites Fenster.</b>
    /// Der Vorläufer öffnete <c>Form_PufferSp_Projekt</c> modal darüber
    /// (<c>btnPufferAnlegen_Click</c>:966-982); seit W10a.4 ist sie eine
    /// Razor-Komponente, und die Hülle reicht ihren Parametersatz
    /// (<c>PufferSpProjektHuelle.Gaben</c>) einfach durch — zwei WebViews übereinander
    /// wären Risiko R2 des Wellenplans.</para>
    ///
    /// <para><b>Zwei Delegaten.</b> <c>Neuladen</c> holt die Pufferliste nach der
    /// Verwaltung neu (sie schreibt sofort), <c>Kapazitaet</c> ist die Formel aus
    /// <c>ProjektPuffer</c> — der Kern führt sie <c>internal</c>, und die Komponente
    /// kennt ihn nicht.</para>
    /// </summary>
    internal static class QuellePufferspeicherHuelle
    {
        // iU9-W10b.1: Der FENSTERWEG dieser Huelle ist entfallen. Ihr einziger
        // Aufrufer war Form_Simulation_Config; seit die Simulationskonfiguration
        // selbst eine Razor-Seite ist, erscheint der Dialog als UEBERLAGERUNG in
        // ihrem Fenster (Risiko R2 - nie zwei WebViews uebereinander). Was bleibt,
        // ist der PARAMETERSATZ unten: Er war von Anfang an fuer genau diesen Tag
        // getrennt gehalten (W10a, "Gaben ohne Geschlossen").

        /// <summary>Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(QuellePufferspeicherDaten daten)
        {
            int idProjekt = daten?.IdProjekt ?? 0;

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Puffer"] = Pufferliste(idProjekt),
                ["Neuladen"] = new Func<IReadOnlyList<QuellPufferzeile>>(
                    () => Pufferliste(idProjekt)),
                ["Kapazitaet"] = new Func<double, double, double>(
                    ProjektPuffer.NutzbareKapazitaetKWh),

                // Die Pufferverwaltung als UEBERLAGERUNG (W10a.4). Keine
                // Verwendungsvorgabe: die Quellseite legt den Kanal nicht fest.
                ["VerwaltungGaben"] = PufferSpProjektHuelle.Gaben(idProjekt, null, 0),

                ["SteuerwertBerechnet"] = DbWerte.WQ_TEMPMODUS_BERECHNET,
                ["SteuerwertFest"] = DbWerte.WQ_TEMPMODUS_FEST,

                ["TitelText"] = MyResource.Resource.SIMQ_PUFFER_TITEL,
                ["TitelMitWp"] = MyResource.Resource.SIMQ_PUFFER_TITEL_MIT_WP,
                ["KopfText"] = MyResource.Resource.SIMQ_PUFFER_KOPF,
                ["GbParameter"] = MyResource.Resource.SIMQ_PUFFER_GB_PARAMETER,
                ["LblQuelltemperatur"] = MyResource.Resource.SIMQ_PUFFER_QUELLTEMPERATUR,
                ["LblSpreizung"] = MyResource.Resource.SIMQ_PUFFER_SPREIZUNG,
                ["LblRegeneration"] = MyResource.Resource.SIMQ_PUFFER_REGENERATION,
                ["UnbegrenztText"] = MyResource.Resource.SIMQ_PUFFER_CB_UNBEGRENZT,
                ["UnbegrenztKonfliktMuster"] =
                    MyResource.Resource.SIMQ_PUFFER_CB_UNBEGRENZT_KONFLIKT,
                ["HinweisQuellwaerme"] = MyResource.Resource.SIMQ_PUFFER_HINWEIS_QUELLWAERME,
                ["HinweisKaskade"] = MyResource.Resource.SIMQ_PUFFER_HINWEIS_KASKADE_KURZ,
                ["HinweisKeinProjektpuffer"] =
                    MyResource.Resource.SIMQ_PUFFER_HINWEIS_KEIN_PROJEKTPUFFER,
                ["HinweisAltbezeichner"] = MyResource.Resource.SIMQ_PUFFER_HINWEIS_ALTBEZEICHNER,
                ["AnzeigeKapazitaet"] = MyResource.Resource.SIMQ_PUFFER_KAPAZITAET,
                ["BtnPufferAnlegen"] = MyResource.Resource.PSP_BTN_PUFFER_ANLEGEN,
                ["VerwaltungTitel"] = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL,
                ["LblTempBezug"] = MyResource.Resource.SIMQ_PUFFER_TEMPERATURBEZUG,
                ["RbBerechnet"] = MyResource.Resource.SIMQ_PUFFER_TB_BERECHNET,
                ["RbFest"] = MyResource.Resource.SIMQ_PUFFER_TB_FEST,
                ["LblTbVorlauf"] = MyResource.Resource.SIMQ_PUFFER_TB_VORLAUF,
                ["LblTbRuecklauf"] = MyResource.Resource.SIMQ_PUFFER_TB_RUECKLAUF,
                ["LblTbHinweis"] = MyResource.Resource.SIMQ_PUFFER_TB_HINWEIS,
                ["LblAnschlusshoehe"] = MyResource.Resource.SIMQ_PUFFER_ANSCHLUSSHOEHE,
                ["LblAnschlusshoeheHinweis"] =
                    MyResource.Resource.SIMQ_PUFFER_ANSCHLUSSHOEHE_HINWEIS,
                ["SpalteSpeicher"] = MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                ["OkText"] = MyResource.Resource.SIM_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,

                ["MsgAuswahl"] = MyResource.Resource.SIMQ_PUFFER_MSG_AUSWAHL,
                ["MsgAnschlusshoehe"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_PUFFER_MSG_ANSCHLUSSHOEHE),
                ["MsgTemperaturpaar"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_PUFFER_MSG_TEMPERATURPAAR),
                ["MsgZahlenwerte"] = MyResource.Resource.PSP_MSG_ZAHLENWERTE,
                ["MsgSpreizung"] = MyResource.Resource.SIMQ_PUFFER_MSG_SPREIZUNG,

                ["HilfeSchluessel"] = "Form_QuellePufferspeicher.btn_Help"
            };
        }

        /// <summary>
        /// Die Projektpuffer, fertig beschriftet — OHNE Verwendungsfilter: Als QUELLE
        /// taugt jeder Speicher des Projekts, die Verwendung steuert nur die Senkenseite
        /// (<c>PufferListeLaden</c>:783-785).
        /// </summary>
        private static IReadOnlyList<QuellPufferzeile> Pufferliste(int idProjekt)
        {
            var l = new List<QuellPufferzeile>();

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(idProjekt, null))
            {
                string verwendung = WaermesenkeClass.VerwendungAnzeige(
                    WaermesenkeClass.WirksameVerwendung(p));

                // Ohne gepflegtes Temperaturpaar die KURZE Form - "0/0 Grad" waere eine
                // Angabe, die es nicht gibt (SpeicherItem.ToString:64-78).
                string anzeige = (p.Vorlauf <= 0 || p.Ruecklauf <= 0)
                    ? string.Format(MyResource.Resource.SIMQ_PUFFER_LISTE_OHNE_TEMP,
                                    p.Bezeichner, verwendung, p.Gesamtvolumen)
                    : string.Format(MyResource.Resource.SIMQ_PUFFER_LISTE_EINTRAG,
                                    p.Bezeichner, verwendung, p.Gesamtvolumen,
                                    p.Vorlauf, p.Ruecklauf);

                string daten = string.Format(MyResource.Resource.SIMQ_PUFFER_DATEN_PROJEKT,
                    verwendung,
                    p.Gesamtvolumen,
                    p.Bereitschaftsverluste.ToString("0.#"),
                    // Einheit und "-" sind Symbole, keine Anzeigetexte (Katalogregel).
                    p.Vorlauf > 0 && p.Ruecklauf > 0
                        ? p.Vorlauf + "/" + p.Ruecklauf + " °C"
                        : "-");

                l.Add(new QuellPufferzeile(p.ID, p.Bezeichner, anzeige, daten, p.Gesamtvolumen));
            }
            return l;
        }
    }
}
