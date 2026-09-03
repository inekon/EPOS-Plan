using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Allgemein;
using EPOS.UI.Dialoge.Simulation;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>PufferSpProjektDialog</c> (iU9-W10a.4) — der Ersatz für
    /// <c>Form_PufferSp_Projekt</c>, die größte Maske dieser Welle.
    ///
    /// <para><b>Ein Delegatensatz für DREI Rollen.</b> Der Dialog erscheint als eigenes
    /// Fenster (aus der Simulationskonfiguration, <c>PufferVerwaltungOeffnen</c>), als
    /// Überlagerung im Quellendialog (W10a.5) und als Überlagerung im Senkendialog
    /// (W10a.7). <see cref="Dienste"/> baut den Satz einmal; <see cref="Gaben"/> liefert
    /// den vollständigen Parametersatz, den die beiden Überlagerungen splatten
    /// (Risiko R‑W10a‑5).</para>
    ///
    /// <para><b>Alle drei Aufrufer ignorieren das <c>DialogResult</c></b> — der Dialog
    /// schreibt sofort (Befund W10‑B29). Zurück kommt der zuletzt angelegte oder
    /// gewählte Speicher; wer ihn nicht braucht, wirft ihn weg.</para>
    /// </summary>
    internal static class PufferSpProjektHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 700 × 662).</summary>
        private static readonly Size MASS = new Size(980, 860);

        /// <summary>
        /// Zeigt den Dialog. Rückgabe: der zuletzt angelegte bzw. gewählte Speicher;
        /// <c>0</c>, wenn keiner (mehr) dasteht.
        /// </summary>
        internal static int Oeffnen(IWin32Window besitzer, int idProjekt, string verwendung,
                                    int idPuffer)
        {
            int ergebnis = idPuffer;
            BlazorDialogForm<PufferSpProjektDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(idProjekt, verwendung, idPuffer, Sprungbruecke.Fuer(null)))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<int>(
                    new object(), id =>
                    {
                        ergebnis = id;
                        if (dlg != null) dlg.Schliessen(true);
                    })
            };

            dlg = new BlazorDialogForm<PufferSpProjektDialog>(Titel(), MASS, werte);
            using (dlg)
            {
                // Die Sprungbruecke braucht das Fenster, ueber dem der Katalog erscheinen
                // soll - das ist der Dialog selbst und steht erst jetzt.
                werte["Sprung"] = Sprungbruecke.Fuer(dlg);
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ergebnis;
        }

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, damit ihn die beiden
        /// Überlagerungen der Wellen W10a.5 und W10a.7 nehmen können.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            int idProjekt, string verwendung, int idPuffer, Func<string, Task<bool>> sprung)
        {
            return new Dictionary<string, object>
            {
                ["IdProjekt"] = idProjekt,
                ["Verwendung"] = verwendung ?? "",
                ["IdPuffer"] = idPuffer,
                ["Dienste"] = Dienste(idProjekt),
                ["Sprung"] = sprung,
                ["SprungzielKatalog"] = Sprungziel.PufferSpAdminNurLesen,
                ["PasstZurVerwendung"] = PasstZurVerwendung(idProjekt, verwendung),
                ["VorbelegteNutzung"] = VorbelegteNutzung(verwendung),

                ["PrioMin"] = Ladeordnung.PRIO_MIN,
                ["PrioMax"] = Ladeordnung.PRIO_MAX,
                ["SchwelleEinVorgabe"] = ProjektPuffer.SCHWELLE_EIN_DEFAULT,
                ["SchwelleAusVorgabe"] = ProjektPuffer.SCHWELLE_AUS_DEFAULT,
                ["SchwelleReserveVorgabe"] = ProjektPuffer.SCHWELLE_RESERVE_DEFAULT,

                ["TitelText"] = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL,
                ["LabelBestand"] = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL,
                ["BtnNeu"] = MyResource.Resource.PSP_BTN_NEUER_PUFFERSPEICHER,
                ["BtnEntfernen"] = MyResource.Resource.PSP_BTN_ENTFERNEN,
                ["BtnKatalog"] = MyResource.Resource.PSP_BTN_KATALOG_ANSEHEN,
                ["GruppeEigenschaften"] = MyResource.Resource.PSP_GRUPPE_EIGENSCHAFTEN,
                ["GruppeSchichtung"] = MyResource.Resource.PSP_GRUPPE_SCHICHTUNG,
                ["GruppeLadereihenfolge"] = MyResource.Resource.PSP_GRUPPE_LADEREIHENFOLGE,
                ["LabelAusKatalog"] = MyResource.Resource.PSP_LABEL_AUS_KATALOG,
                ["KatalogFreieEingabe"] = MyResource.Resource.PSP_KATALOG_FREIE_EINGABE,
                ["LabelBezeichner"] = MyResource.Resource.PSP_LABEL_BEZEICHNER,
                ["LabelVerwendung"] = MyResource.Resource.PSP_LABEL_VERWENDUNG,
                ["LabelGesamtvolumen"] = MyResource.Resource.PSP_LABEL_GESAMTVOLUMEN,
                ["LabelBereitschaftsverluste"] = MyResource.Resource.PSP_LABEL_BEREITSCHAFTSVERLUSTE,
                ["LabelVorlauf"] = MyResource.Resource.PSP_LABEL_VORLAUF,
                ["LabelRuecklauf"] = MyResource.Resource.PSP_LABEL_RUECKLAUF,
                ["LabelEinschaltschwelle"] = MyResource.Resource.PSP_LABEL_EINSCHALTSCHWELLE,
                ["LabelAbschaltschwelle"] = MyResource.Resource.PSP_LABEL_ABSCHALTSCHWELLE,
                ["LabelSchwelleNachrangig"] = MyResource.Resource.PSP_LABEL_SCHWELLE_NACHRANGIG,
                ["LabelMindestfuellstand"] = MyResource.Resource.PSP_LABEL_MINDESTFUELLSTAND,
                ["LabelSchichten"] = MyResource.Resource.PSP_LABEL_SCHICHTEN,
                ["LabelHoehe"] = MyResource.Resource.PSP_LABEL_HOEHE,
                ["LabelLambda"] = MyResource.Resource.PSP_LABEL_LAMBDA_EFF,
                ["LabelTNutzBW"] = MyResource.Resource.PSP_LABEL_T_NUTZ_BW,
                ["LabelEntnahmeKopf"] = MyResource.Resource.PSP_LABEL_ENTNAHMEHOEHEN,
                ["LabelLadeleistung"] = MyResource.Resource.PSP_LABEL_LADELEISTUNG_MAX,
                ["LabelEntladeleistung"] = MyResource.Resource.PSP_LABEL_ENTLADELEISTUNG_MAX,
                ["LabelEntladeprioritaet"] = MyResource.Resource.PSP_LABEL_ENTLADEPRIORITAET,
                ["PrioAutomatisch"] = MyResource.Resource.PSP_PRIO_AUTOMATISCH,
                ["SpalteAnlage"] = MyResource.Resource.SIM_SPALTE_ANLAGE,
                ["SpalteErzeuger"] = MyResource.Resource.SIM_ERZEUGERNAME_ALLGEMEIN,
                ["SpalteSenke"] = MyResource.Resource.SIM_SPALTE_SENKE,
                ["SpalteLadeprio"] = MyResource.Resource.PSP_SPALTE_LADEPRIO,
                ["SpalteLaedtBis"] = MyResource.Resource.PSP_SPALTE_LAEDT_BIS,
                ["BtnAnlegen"] = MyResource.Resource.PSP_BTN_ANLEGEN,
                ["BtnUebernehmen"] = MyResource.Resource.PSP_BTN_UEBERNEHMEN,
                ["BtnSchliessen"] = MyResource.Resource.PSP_BTN_SCHLIESSEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["NutzungHeizung"] = MyResource.Resource.KANAL_HEIZUNG_ANZEIGE,
                ["NutzungBrauchwasser"] = MyResource.Resource.KANAL_BRAUCHWASSER_ANZEIGE,
                ["NutzungProzess"] = MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                ["HerleitungVerwendung"] = MyResource.Resource.PSP_HERLEITUNG_VERWENDUNG,

                ["AnzeigeQmax"] = MyResource.Resource.PSP_ANZEIGE_QMAX,
                ["LadenNochNichtAngelegt"] = MyResource.Resource.PSP_LADEN_NOCH_NICHT_ANGELEGT,
                ["LadenKeineAnlage"] = MyResource.Resource.PSP_LADEN_KEINE_ANLAGE,

                ["FehlerBezeichnerFehlt"] = MyResource.Resource.PSP_FEHLER_BEZEICHNER_FEHLT,
                ["FehlerKlassenSetLeer"] = MyResource.Resource.PSP_FEHLER_KLASSENSET_LEER,
                ["FehlerVolumen"] = MyResource.Resource.PSP_FEHLER_VOLUMEN,
                ["FehlerVerluste"] = MyResource.Resource.PSP_FEHLER_VERLUSTE,
                ["FehlerSchwelleZahl"] = MyResource.Resource.PSP_FEHLER_SCHWELLE_ZAHL,
                ["FehlerSchwelleBereich"] = MyResource.Resource.PSP_FEHLER_SCHWELLE_BEREICH,
                ["FehlerEinKleinerAus"] = MyResource.Resource.PSP_FEHLER_EIN_KLEINER_AUS,
                ["FehlerNachrangUeberAus"] = MyResource.Resource.PSP_FEHLER_NACHRANG_UEBER_AUS,
                ["FehlerNachrangUnterEin"] = MyResource.Resource.PSP_FEHLER_NACHRANG_UNTER_EIN,
                ["FehlerReserveUeberAus"] = MyResource.Resource.PSP_FEHLER_RESERVE_UEBER_AUS,
                ["FehlerHoehe"] = MyResource.Resource.PSP_FEHLER_HOEHE,
                ["FehlerLambdaEff"] = MyResource.Resource.PSP_FEHLER_LAMBDA_EFF,
                ["FehlerTNutzBW"] = MyResource.Resource.PSP_FEHLER_T_NUTZ_BW,
                ["FehlerEntnahmehoehe"] = MyResource.Resource.PSP_FEHLER_ENTNAHMEHOEHE,
                ["FehlerLeistung"] = MyResource.Resource.PSP_FEHLER_LEISTUNG,
                ["FehlerSchichtungAmVerbund"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.PSP_FEHLER_SCHICHTUNG_AM_VERBUND),

                ["NameEinschaltschwelle"] = MyResource.Resource.PSP_NAME_EINSCHALTSCHWELLE,
                ["NameAbschaltschwelle"] = MyResource.Resource.PSP_NAME_ABSCHALTSCHWELLE,
                ["NameAbschaltschwelleNachrang"] = MyResource.Resource.PSP_NAME_ABSCHALTSCHWELLE_NACHRANG,
                ["NameMindestfuellstand"] = MyResource.Resource.PSP_NAME_MINDESTFUELLSTAND,
                ["NameLadeleistungMax"] = MyResource.Resource.PSP_NAME_LADELEISTUNG_MAX,
                ["NameEntladeleistungMax"] = MyResource.Resource.PSP_NAME_ENTLADELEISTUNG_MAX,

                ["MeldungAnlegenFehlgeschlagen"] = MyResource.Resource.PSP_MELDUNG_ANLEGEN_FEHLGESCHLAGEN,
                ["MeldungAendernFehlgeschlagen"] = MyResource.Resource.PSP_MELDUNG_AENDERN_FEHLGESCHLAGEN,
                ["MeldungEntfernenFehlgeschlagen"] = MyResource.Resource.PSP_MELDUNG_ENTFERNEN_FEHLGESCHLAGEN,
                ["MeldungEntfernenBlockiert"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.PSP_MELDUNG_ENTFERNEN_BLOCKIERT),
                ["MeldungEntfernenBestaetigen"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.PSP_MELDUNG_ENTFERNEN_BESTAETIGEN),
                ["MeldungKlassensetwechsel"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.PSP_MELDUNG_KLASSENSETWECHSEL),
                ["TitelKlassenSetAendern"] = MyResource.Resource.PSP_TITEL_KLASSENSET_AENDERN,
                ["TitelPufferEntfernen"] = MyResource.Resource.PSP_TITEL_PUFFER_ENTFERNEN,

                ["StatusAngelegt"] = MyResource.Resource.PSP_STATUS_ANGELEGT,
                ["StatusAenderungenUebernommen"] = MyResource.Resource.PSP_STATUS_AENDERUNGEN_UEBERNOMMEN,
                ["StatusEntfernt"] = MyResource.Resource.PSP_STATUS_ENTFERNT,

                ["HilfeSchluessel"] = "Form_PufferSp_Projekt.btn_Help"
            };
        }

        // =============================================================================
        // Die Datenseite - sechzehn Delegaten
        // =============================================================================

        /// <summary>
        /// Baut den Delegatensatz für EIN Projekt. Der Katalog wird bei jedem Aufruf neu
        /// gelesen — nach dem Sprung in die Katalogverwaltung kann er sich geändert
        /// haben (<c>btnKatalog_Click</c>:1593-1603).
        /// </summary>
        internal static PufferSpProjektDienste Dienste(int idProjekt)
        {
            return new PufferSpProjektDienste(
                Katalogzeilen: () => PufferSpStammCtrl.Katalogzeilen()
                    .Select(z => new PspKatalogzeile(z.Id, z.Bezeichner, z.Gesamtvolumen,
                                                     z.Bereitschaftsverluste))
                    .ToList(),

                Projektliste: () => Projektliste(idProjekt),

                PufferLesen: id => Pufferstand(id),

                Systemvorgaben: () => (PufferSpCtrl.SystemVorlauf(idProjekt),
                                       PufferSpCtrl.SystemRuecklauf(idProjekt)),

                Ladereihenfolge: id => Ladereihenfolge(idProjekt, id),

                Automatiktext: id => Automatiktext(idProjekt, id),

                Entladeposition: (id, h, b, p) => Entladeposition(idProjekt, id, h, b, p),

                KlassenSetAnzeige: (h, b, p) =>
                    Warnkriterien.KlassenSetAnzeige(new PufferSpCtrl.KlassenSet(h, b, p)),

                IstLeitspeicher: AnlagePufferVerbundCtrl.IstLeitspeicher,

                Referenzen: id => PufferSpCtrl.ReferenzenAufPuffer(id),

                TemperaturenPruefen: (vorlauf, ruecklauf) =>
                {
                    int v, r;
                    string fehler;
                    return ProjektPuffer.TemperaturenPruefen(vorlauf, ruecklauf, out v, out r,
                                                             out fehler)
                        ? null : fehler;
                },

                Anlegen: e => Anlegen(idProjekt, e),

                Aendern: (id, e) => Aendern(idProjekt, id, e),

                Entfernen: id => PufferSpCtrl.ProjektPufferEntfernen(id, idProjekt),

                Klemmhinweis: (id, e) => Klemmhinweis(e),

                Kapazitaet: ProjektPuffer.NutzbareKapazitaetKWh);
        }

        /// <summary>Die Projektliste, fertig beschriftet (<c>ProjektlisteLaden</c>:1151-1166).</summary>
        private static IReadOnlyList<PspProjektzeile> Projektliste(int idProjekt)
        {
            var l = new List<PspProjektzeile>();
            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(idProjekt, null))
            {
                l.Add(new PspProjektzeile(p.ID,
                    string.Format(MyResource.Resource.PSP_LISTE_EINTRAG,
                                  p.Bezeichner,
                                  WaermesenkeClass.VerwendungAnzeige(
                                      WaermesenkeClass.WirksameVerwendung(p)),
                                  p.Gesamtvolumen) +
                    (p.VerwendungFehlt ? MyResource.Resource.PSP_LISTE_VERWENDUNG_FEHLT : "")));
            }
            return l;
        }

        /// <summary>
        /// Der vollständige Stand eines Puffers. Klassen-Set und Schichtdaten kommen aus
        /// der DATENBANK und nicht aus <c>PufferInfo</c>: Deren Datensatz führt bis zur
        /// Engine-Umstellung nur die Verwendung (<c>PufferAnzeigen</c>:1242-1300).
        /// </summary>
        private static PspPufferstand Pufferstand(int idPuffer)
        {
            WaermesenkeClass.PufferInfo p = WaermesenkeClass.PufferLesen(idPuffer);
            if (p == null) return null;

            PufferSpCtrl.KlassenSet set = PufferSpCtrl.KlassenSetLesen(idPuffer);
            PufferSpCtrl.Schichtdaten s = PufferSpCtrl.SchichtdatenLesen(idPuffer);

            return new PspPufferstand(
                p.ID, p.Bezeichner, p.Gesamtvolumen, p.Bereitschaftsverluste,
                p.Vorlauf, p.Ruecklauf,
                p.SchwelleEin, p.SchwelleAus, p.SchwelleAusNachrang, p.SchwelleReserve,
                p.Entladeprio,
                set.Heizung, set.Brauchwasser, set.Prozess,
                Schichtdaten(s));
        }

        private static PspSchichtdaten Schichtdaten(PufferSpCtrl.Schichtdaten s)
        {
            if (s == null) return new PspSchichtdaten();
            return new PspSchichtdaten(
                PufferSpCtrl.SchichtenKlemmen(s.Schichten), s.Hoehe, s.LambdaEff, s.TNutzBW,
                s.EntnahmeHeizung, s.EntnahmeBW, s.EntnahmeProzess,
                s.LadeleistungMax, s.EntladeleistungMax);
        }

        private static PufferSpCtrl.Schichtdaten Schichtdaten(PspSchichtdaten s)
        {
            var d = new PufferSpCtrl.Schichtdaten();
            if (s == null) return d;

            d.Schichten = s.Schichten;
            d.Hoehe = s.Hoehe;
            d.LambdaEff = s.LambdaEff;
            d.TNutzBW = s.TNutzBW;
            d.EntnahmeHeizung = s.EntnahmeHeizung;
            d.EntnahmeBW = s.EntnahmeBW;
            d.EntnahmeProzess = s.EntnahmeProzess;
            d.LadeleistungMax = s.LadeleistungMax;
            d.EntladeleistungMax = s.EntladeleistungMax;
            return d;
        }

        /// <summary>
        /// Die Ladereihenfolge, fertig als sechs Textspalten
        /// (<c>LadereihenfolgeAnzeigen</c>:1384-1429).
        /// </summary>
        private static IReadOnlyList<PspLadezeile> Ladereihenfolge(int idProjekt, int idPuffer)
        {
            var l = new List<PspLadezeile>();
            List<Ladeordnung.LadeEintrag> liste = Ladeordnung.Ladereihenfolge(idProjekt, idPuffer);

            for (int i = 0; i < liste.Count; i++)
            {
                Ladeordnung.LadeEintrag e = liste[i];

                string ladeprio = e.PrioManuell
                    ? string.Format(MyResource.Resource.PSP_LADEPRIO_MANUELL, e.Ladeprio)
                    : e.Ladeprio.ToString();
                string obergrenze = e.ObergrenzeEigen
                    ? string.Format(MyResource.Resource.PSP_OBERGRENZE_EIGEN,
                                    e.Obergrenze.ToString("0.#"))
                    : e.Obergrenze.ToString("0.#") + " %";

                l.Add(new PspLadezeile(
                    (i + 1) + ".",
                    e.Bezeichner,
                    e.Erzeuger,
                    e.Zweitsenke ? MyResource.Resource.SIM_SPALTE_ZWEITSENKE
                                 : MyResource.Resource.SIM_GRUPPE_HAUPTSENKE,
                    ladeprio,
                    obergrenze));
            }
            return l;
        }

        /// <summary>Die Zeile „Entladepriorität automatisch: n" (<c>AutomatikTextSetzen</c>).</summary>
        private static string Automatiktext(int idProjekt, int idPuffer)
        {
            int automatik = Ladeordnung.EntladeprioAutomatik(idProjekt, idPuffer);
            return string.Format(MyResource.Resource.PSP_PRIO_AUTOMATISCH_WERT, automatik);
        }

        /// <summary>
        /// „Wird als n. von m … entladen" — beim KOMBISPEICHER zwei Zeilen, je Kanal
        /// eine (<c>EntladungAnzeigen</c>:1431-1465, <c>KombiPositionstext</c>).
        /// </summary>
        private static string Entladeposition(int idProjekt, int idPuffer,
                                              bool heizung, bool brauchwasser, bool prozess)
        {
            var set = new PufferSpCtrl.KlassenSet(heizung, brauchwasser, prozess);
            string verwendung = set.Verwendung;
            if (string.IsNullOrEmpty(verwendung)) verwendung = WaermesenkeClass.VERWENDUNG_HEIZUNG;

            if (WaermesenkeClass.IstKombiVerwendung(verwendung))
            {
                string h = Kanalzeile(idProjekt, idPuffer, WaermesenkeClass.VERWENDUNG_HEIZUNG,
                                      MyResource.Resource.PSP_ENTLADE_POSITION_KANAL_HEIZUNG);
                string b = Kanalzeile(idProjekt, idPuffer, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                      MyResource.Resource.PSP_ENTLADE_POSITION_KANAL_WARMWASSER);
                return string.Join(Environment.NewLine,
                                   new[] { h, b }.Where(z => z.Length > 0));
            }

            List<Ladeordnung.EntladeEintrag> reihe =
                Ladeordnung.Entladereihenfolge(idProjekt, verwendung);
            int pos = Ladeordnung.Position(reihe, idPuffer);

            return pos > 0
                ? string.Format(MyResource.Resource.PSP_ENTLADE_POSITION,
                                pos, reihe.Count, Speicherwort(verwendung, reihe.Count))
                : "";
        }

        /// <summary>
        /// Eine Kanalzeile fuer den Kombispeicher; "" = in diesem Kanal nicht enthalten
        /// (<c>KanalPositionstext</c>:1494-1501).
        /// </summary>
        private static string Kanalzeile(int idProjekt, int idPuffer, string kanal, string muster)
        {
            List<Ladeordnung.EntladeEintrag> reihe =
                Ladeordnung.Entladereihenfolge(idProjekt, kanal);
            int pos = Ladeordnung.Position(reihe, idPuffer);

            return pos > 0 ? string.Format(muster, pos, reihe.Count) : "";
        }

        /// <summary>
        /// Der Kanalname in der grammatisch richtigen Form - Singular und Plural sind je
        /// EIGENE Ressourcen (<c>KanalSpeicherWort</c>:1510-1526).
        /// </summary>
        private static string Speicherwort(string verwendung, int anzahl)
        {
            bool brauchwasser = string.Equals(verwendung,
                                              WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                              StringComparison.OrdinalIgnoreCase);
            if (brauchwasser)
                return anzahl == 1
                    ? MyResource.Resource.PSP_KANALWORT_BRAUCHWASSERSPEICHER
                    : MyResource.Resource.PSP_KANALWORT_BRAUCHWASSERSPEICHER_PLURAL;

            return anzahl == 1
                ? MyResource.Resource.PSP_KANALWORT_HEIZUNGSSPEICHER
                : MyResource.Resource.PSP_KANALWORT_HEIZUNGSSPEICHER_PLURAL;
        }

        // --- Schreiben ---------------------------------------------------------------

        private static int Anlegen(int idProjekt, PspEingaben e)
        {
            string hersteller = "", speichertyp = ProjektPuffer.SPEICHERTYP_PUFFER;
            double investition = 0;
            KatalogfelderLesen(e.Katalogzeile, 0, idProjekt, ref hersteller, ref speichertyp,
                               ref investition);

            var set = new PufferSpCtrl.KlassenSet(e.Heizung, e.Brauchwasser, e.Prozess);

            return PufferSpCtrl.ProjektPufferAnlegen(
                idProjekt, e.Bezeichner, hersteller, speichertyp, e.Volumen, e.Verluste,
                investition, set.Verwendung, e.Vorlauf, e.Ruecklauf,
                e.SchwelleEin, e.SchwelleAus, e.SchwelleNachrang, e.Entladeprio,
                e.SchwelleReserve, set.Heizung, set.Brauchwasser, set.Prozess,
                Schichtdaten(e.Schicht));
        }

        private static bool Aendern(int idProjekt, int idPuffer, PspEingaben e)
        {
            string hersteller = "", speichertyp = ProjektPuffer.SPEICHERTYP_PUFFER;
            double investition = 0;
            KatalogfelderLesen(e.Katalogzeile, idPuffer, idProjekt, ref hersteller,
                               ref speichertyp, ref investition);

            var set = new PufferSpCtrl.KlassenSet(e.Heizung, e.Brauchwasser, e.Prozess);

            return PufferSpCtrl.ProjektPufferAendern(
                idPuffer, idProjekt, e.Bezeichner, hersteller, speichertyp, e.Volumen,
                e.Verluste, investition, set.Verwendung, e.Vorlauf, e.Ruecklauf,
                e.SchwelleEin, e.SchwelleAus, e.SchwelleNachrang, e.Entladeprio,
                e.SchwelleReserve, set.Heizung, set.Brauchwasser, set.Prozess,
                Schichtdaten(e.Schicht));
        }

        /// <summary>
        /// Hersteller, Speichertyp und Investitionskosten — aus der gewählten
        /// Katalogzeile oder aus dem Bestand (<c>KatalogfelderLesen</c>:1882-1913).
        /// </summary>
        private static void KatalogfelderLesen(int katalogzeile, int idPuffer, int idProjekt,
                                               ref string hersteller, ref string speichertyp,
                                               ref double investition)
        {
            if (katalogzeile >= 0)
            {
                IReadOnlyList<PufferSpStammCtrl.Katalogzeile> katalog =
                    PufferSpStammCtrl.Katalogzeilen();
                if (katalogzeile < katalog.Count)
                {
                    PufferSpStammCtrl.Katalogzeile z = katalog[katalogzeile];
                    hersteller = z.Hersteller;
                    if (z.Speichertyp.Length > 0) speichertyp = z.Speichertyp;
                    investition = z.Investitionskosten;
                    return;
                }
            }

            if (idPuffer <= 0) return;

            PufferSpStammCtrl.SpeicherDetail d = PufferSpCtrl.Detail(idPuffer, idProjekt);
            if (d == null) return;

            hersteller = d.Hersteller;
            if (!string.IsNullOrEmpty(d.Typ)) speichertyp = d.Typ;
            double.TryParse(d.Investitionskosten, NumberStyles.Float, CultureInfo.CurrentCulture,
                            out investition);
        }

        /// <summary>
        /// KRITERIUM W4 — der WEICHE Hinweis NACH dem Speichern
        /// (<c>KlemmhinweisZeigen</c>:1740-1758). <c>null</c> = nichts zu sagen.
        /// </summary>
        private static string Klemmhinweis(PspEingaben e)
        {
            if (e?.Schicht?.TNutzBW == null) return null;

            double tNutz = e.Schicht.TNutzBW.Value;
            double vlEff = Warnkriterien.WirksamerVorlauf(e.Vorlauf ?? 0, e.Ruecklauf ?? 0,
                                                          e.Bezeichner);
            if (tNutz <= vlEff) return null;

            return MyResource.Resource.SIMWARN_DIALOG_KOPF + Environment.NewLine +
                   Environment.NewLine + "  • " +
                   string.Format(MyResource.Resource.SIMWARN_W4_TNUTZ_UEBER_VLEFF,
                                 e.Bezeichner,
                                 tNutz.ToString("0.#", CultureInfo.CurrentCulture),
                                 vlEff.ToString("0.#", CultureInfo.CurrentCulture));
        }

        // --- Vorwahl ------------------------------------------------------------------

        /// <summary>
        /// Die Ids der Speicher, die zur gewünschten Verwendung passen — die Regel aus
        /// <c>ErsterMitVerwendung</c>:1123-1135. Eine leere Verwendung am Puffer zählt
        /// als „Heizung".
        /// </summary>
        private static IReadOnlyList<int> PasstZurVerwendung(int idProjekt, string verwendung)
        {
            var l = new List<int>();
            if (string.IsNullOrEmpty(verwendung)) return l;

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(idProjekt, null))
            {
                if (string.Equals(WaermesenkeClass.WirksameVerwendung(p), verwendung,
                                  StringComparison.OrdinalIgnoreCase))
                    l.Add(p.ID);
            }
            return l;
        }

        /// <summary>
        /// Die Nutzung, mit der ein NEUER Speicher aufgeht (<c>NeuVorbereiten</c>
        /// :1193-1205): Kombi → {Heizung, Brauchwasser}, Brauchwasser → {Brauchwasser},
        /// sonst {Heizung}.
        /// </summary>
        private static IReadOnlyList<int> VorbelegteNutzung(string verwendung)
        {
            string wunsch =
                WaermesenkeClass.IstKombiVerwendung(verwendung)
                    ? WaermesenkeClass.VERWENDUNG_KOMBI
                    : string.Equals(verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                    StringComparison.OrdinalIgnoreCase)
                        ? WaermesenkeClass.VERWENDUNG_BRAUCHWASSER
                        : WaermesenkeClass.VERWENDUNG_HEIZUNG;

            PufferSpCtrl.KlassenSet set = PufferSpCtrl.KlassenSetAusVerwendung(wunsch);
            var l = new List<int>();
            if (set.Heizung) l.Add(0);
            if (set.Brauchwasser) l.Add(1);
            if (set.Prozess) l.Add(2);
            return l;
        }

        private static string Titel()
        {
            return MyResource.Resource.PSP_PROJEKT_FENSTERTITEL;
        }
    }
}
