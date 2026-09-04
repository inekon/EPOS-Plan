using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Simulation;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE von <c>WaermesenkeDialog</c> (iU9-W10a.7) — der Ersatz für
    /// <c>Form_Waermesenke</c>, die zweitgrößte Maske der Welle.
    ///
    /// <para><b>Der Dialog SPEICHERT selbst</b> — Senkenliste nach <c>Z_AnlageSenke</c>
    /// und Verbundmitglieder nach <c>Z_AnlagePufferVerbund</c>, in EINEM Zug
    /// (<c>ListeSpeichern</c>:2018-2034). Die Mitgliederliste geht IMMER heraus, auch
    /// leer: Das ist der Weg, auf dem ein Verbund wieder aufgelöst wird.</para>
    ///
    /// <para><b>Die Pufferverwaltung ist eine ÜBERLAGERUNG</b> (Risiko R2). Ihre
    /// Verwendung hängt am gerade gewählten Ziel; deshalb kommt der Parametersatz als
    /// <c>Func&lt;string, …&gt;</c> herein und wird erst beim Öffnen gebaut.</para>
    /// </summary>
    internal static class WaermesenkeHuelle
    {
        // iU9-W10b.1: Der FENSTERWEG dieser Huelle ist entfallen. Ihr einziger
        // Aufrufer war Form_Simulation_Config; seit die Simulationskonfiguration
        // selbst eine Razor-Seite ist, erscheint der Dialog als UEBERLAGERUNG in
        // ihrem Fenster (Risiko R2 - nie zwei WebViews uebereinander). Was bleibt,
        // ist der PARAMETERSATZ unten: Er war von Anfang an fuer genau diesen Tag
        // getrennt gehalten (W10a, "Gaben ohne Geschlossen").

        /// <summary>Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(WaermesenkeDaten daten)
        {
            int idProjekt = daten?.IdProjekt ?? 0;
            int idType = daten?.IdType ?? 0;

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Dienste"] = Dienste(daten),

                // Die sechs Ziele in KONZEPTREIHENFOLGE (ZielListeFuellen:797-831).
                ["Ziele"] = new[]
                {
                    DbWerte.WS_ZIEL_HEIZKREIS,
                    DbWerte.WS_ZIEL_PROZESS,
                    DbWerte.WS_ZIEL_PUFFER_HEIZUNG,
                    DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER,
                    DbWerte.WS_ZIEL_PUFFER_PROZESS,
                    DbWerte.WS_ZIEL_PUFFER_KOMBI
                },
                ["Zieltexte"] = new[]
                {
                    MyResource.Resource.SIM_RB_HEIZKREIS,
                    MyResource.Resource.KANAL_PROZESS_ANZEIGE,
                    MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_HEIZUNG,
                    MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_BRAUCHWASSER,
                    MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_PROZESS,
                    MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_KOMBI
                },
                ["Pufferziele"] = new[]
                {
                    DbWerte.WS_ZIEL_PUFFER_HEIZUNG,
                    DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER,
                    DbWerte.WS_ZIEL_PUFFER_PROZESS,
                    DbWerte.WS_ZIEL_PUFFER_KOMBI
                },
                ["ZielHeizkreis"] = DbWerte.WS_ZIEL_HEIZKREIS,
                ["ZielPufferHeizung"] = DbWerte.WS_ZIEL_PUFFER_HEIZUNG,

                ["Bedarfsarten"] = new[]
                {
                    WaermequelleClass.SENKE_BEIDES,
                    WaermequelleClass.SENKE_WARMWASSER,
                    WaermequelleClass.SENKE_HEIZUNG
                },
                ["Bedarfsarttexte"] = new[]
                {
                    MyResource.Resource.SIM_BEDARF_BEIDES,
                    MyResource.Resource.SIM_BEDARF_WARMWASSER,
                    MyResource.Resource.SIM_BEDARF_HEIZWAERME
                },

                ["PrioMin"] = Ladeordnung.PRIO_MIN,
                ["PrioMax"] = Ladeordnung.PRIO_MAX,

                ["VerwaltungGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    verwendung => PufferSpProjektHuelle.Gaben(idProjekt, verwendung, 0, null)),
                ["VerwendungZuZiel"] = new Func<string, string>(VerwendungZuZiel),

                ["TitelText"] = MyResource.Resource.SIM_SENKE_TITEL,
                ["TitelMitAnlage"] = MyResource.Resource.SIM_SENKE_TITEL_ANLAGE,
                ["GruppeSenkenliste"] = MyResource.Resource.SIM_GRUPPE_SENKENLISTE,
                ["GruppeSenkenzeile"] = MyResource.Resource.SIM_GRUPPE_SENKENZEILE,
                ["GruppeVerbund"] = MyResource.Resource.SIM_GB_VERBUND,
                ["GruppeLadeverhalten"] = MyResource.Resource.SIM_GB_LADEVERHALTEN,
                ["SpalteRang"] = MyResource.Resource.SIM_SPALTE_RANG,
                ["SpalteZiel"] = MyResource.Resource.SIM_SPALTE_ZIEL,
                ["SpalteSpeicher"] = MyResource.Resource.SIM_SPALTE_SPEICHER,
                ["SpalteBedarfsart"] = MyResource.Resource.SIM_SPALTE_BEDARFSART,
                ["SpalteLaden"] = MyResource.Resource.SIM_SPALTE_LADEN,
                ["SpalteEinspeisehoehe"] = MyResource.Resource.SIM_SPALTE_EINSPEISEHOEHE,
                ["BtnHinzu"] = MyResource.Resource.SIM_BTN_SENKE_HINZU,
                ["BtnEntfernen"] = MyResource.Resource.SIM_BTN_SENKE_ENTFERNEN,
                ["BtnRauf"] = MyResource.Resource.SIM_BTN_SENKE_RAUF,
                ["BtnRunter"] = MyResource.Resource.SIM_BTN_SENKE_RUNTER,
                ["BtnPufferAnlegen"] = MyResource.Resource.PSP_BTN_PUFFER_ANLEGEN,
                ["VerwaltungTitel"] = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL,
                ["LabelZiel"] = MyResource.Resource.SIM_SPALTE_ZIEL,
                ["LabelSpeicher"] = MyResource.Resource.SIM_SPALTE_SPEICHER,
                ["LabelBedarfsart"] = MyResource.Resource.SIM_SPALTE_BEDARFSART,
                ["LabelLadeprio"] = MyResource.Resource.PSP_SPALTE_LADEPRIO,
                ["LabelLadeprioPv"] = MyResource.Resource.SIM_LBL_PV_UEBERSCHUSS,
                ["LabelLadegrenze"] = MyResource.Resource.SIM_CHK_LADEGRENZE,
                ["LabelEinspeisehoehe"] = MyResource.Resource.SIM_CHK_EINSPEISEHOEHE,
                // Der Eintrag 0 der Ladeprioritaeten nennt die Vorgabe und den Erzeugernamen;
                // die PV-Liste sagt dort "unveraendert" (PrioListeFuellen:1590-1608).
                ["PrioNachVorgabe"] = string.Format(MyResource.Resource.SIM_PRIO_VORGABE,
                                                    Ladeordnung.VorgabeLadeprio(idType),
                                                    Ladeordnung.ErzeugerName(idType)),
                ["PrioPvUnveraendert"] = MyResource.Resource.SIM_PRIO_UNVERAENDERT,
                ["HinweisBedarf"] = MyResource.Resource.SIM_LBL_BEDARF_HINWEIS,
                ["HinweisPuffer"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_LBL_HINWEIS_PUFFER),
                ["TipEinspeisehoehe"] = MyResource.Resource.SIM_TIP_EINSPEISEHOEHE,
                ["VerbundSumme"] = MyResource.Resource.SIM_VERBUND_SUMME,
                ["VerbundKeinVerbund"] = MyResource.Resource.SIM_VERBUND_KEIN_VERBUND,
                ["PositionLaedtAls"] = MyResource.Resource.SIM_POSITION_LAEDT_ALS,
                ["OkText"] = MyResource.Resource.SIM_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["HerleitungSpeicherliste"] = MyResource.Resource.SIM_HERLEITUNG_SPEICHERLISTE,
                ["HerleitungVerbundliste"] = MyResource.Resource.SIM_HERLEITUNG_VERBUNDLISTE,

                ["MsgLetzteZeile"] = MyResource.Resource.SIM_MSG_SENKE_LETZTE_ZEILE,
                ["MsgLadegrenzeZahl"] = MyResource.Resource.SIM_MSG_LADEGRENZE_ZAHL,
                ["MsgLadegrenzeBereich"] = MyResource.Resource.SIM_MSG_LADEGRENZE_BEREICH,
                ["MsgEinspeisehoeheZahl"] = MyResource.Resource.SIM_MSG_EINSPEISEHOEHE_ZAHL,
                ["MsgEinspeisehoeheBereich"] = MyResource.Resource.SIM_MSG_EINSPEISEHOEHE_BEREICH,
                ["MsgPufferFehlt"] = MyResource.Resource.SIM_MSG_SENKE_PUFFER_FEHLT,
                ["MsgDoppelt"] = MyResource.Resource.SIM_MSG_SENKE_DOPPELT,
                ["RolleRang"] = MyResource.Resource.SIM_ROLLE_RANG,
                ["MsgPufferAnlegenFrage"] =
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIM_MSG_PUFFER_ANLEGEN_FRAGE),
                ["TitelPufferFehlt"] = MyResource.Resource.SIM_TITEL_SENKE_PUFFER_FEHLT,
                ["WarnKopf"] = MyResource.Resource.SIMWARN_DIALOG_KOPF,

                ["HilfeSchluessel"] = "Form_Waermesenke.btn_Help"
            };
        }

        // =============================================================================
        // Die Datenseite
        // =============================================================================

        internal static WaermesenkeDienste Dienste(WaermesenkeDaten daten)
        {
            int idProjekt = daten?.IdProjekt ?? 0;
            int idAnlage = daten?.IdAnlage ?? 0;
            int idType = daten?.IdType ?? 0;

            return new WaermesenkeDienste(
                Zeilen: () => Zeilen(idAnlage),
                Puffer: () => Pufferliste(idProjekt, null),
                VerbundKandidaten: ziel => Pufferliste(idProjekt, VerwendungZuZiel(ziel)),
                VerbundKapazitaet: (idLeit, mitglieder) =>
                    WaermesenkeClass.VerbundKapazitaet(idLeit, new List<int>(mitglieder)),
                Position: (z, zweitsenke) => Position(idProjekt, idAnlage, idType, z, zweitsenke),
                PufferName: WaermesenkeClass.PufferName,
                ZielAnzeige: WaermesenkeClass.ZielAnzeigeVollstaendig,
                HarterBefund: zeilen =>
                {
                    Warnbefund hart = Warnkriterien.ErsterHarter(
                        Warnkriterien.PruefeSenken(idProjekt, idAnlage, Modelle(idAnlage, zeilen)));
                    return hart != null ? Zeilenumbruch.Normalisieren(hart.Text) : null;
                },
                Pruefen: (zeilen, verbund) =>
                {
                    WaermesenkeClass.PruefErgebnis erg = WaermesenkeClass.Pruefen(
                        idProjekt, idAnlage, Modelle(idAnlage, zeilen), new List<int>(verbund));
                    return new SenkenPruefung(erg.Ok, erg.Fehler ?? "",
                                              erg.AbsprungPufferVerwaltung, erg.Warnung ?? "");
                },
                Schreiben: (zeilen, verbund) => Schreiben(idAnlage, zeilen, verbund),
                WeicheBefunde: zeilen =>
                {
                    var l = new List<string>();
                    foreach (Warnbefund b in Warnkriterien.NurWeiche(
                                 Warnkriterien.PruefeSenken(idProjekt, idAnlage,
                                                            Modelle(idAnlage, zeilen))))
                        l.Add(Zeilenumbruch.Einzeilig(b.Text));
                    return l;
                });
        }

        /// <summary>
        /// Die gespeicherte Senkenliste (<c>ZeilenLaden</c>:887-901). Ohne die Spalte
        /// oder ohne Anlage bleibt sie leer; die Rang-1-Invariante setzt die Komponente.
        /// </summary>
        private static IReadOnlyList<SenkenzeileDaten> Zeilen(int idAnlage)
        {
            var l = new List<SenkenzeileDaten>();
            if (!Z_AnlageSenkeCtrl.SpalteVorhanden() || idAnlage <= 0) return l;

            List<Z_AnlageSenkeModel> gelesen = new Z_AnlageSenkeCtrl().LesenJeAnlage(idAnlage);
            if (gelesen == null) return l;

            foreach (Z_AnlageSenkeModel z in gelesen)
            {
                if (z == null) continue;
                l.Add(new SenkenzeileDaten
                {
                    Ziel = z.Ziel,
                    IdPuffer = z.ID_Puffer,
                    Bedarfsart = z.Bedarfsart,
                    Ladeprio = z.Ladeprio,
                    // 0 heisst "keine eigene Obergrenze" - in der Komponente null.
                    Ladegrenze = z.Ladegrenze > 0 ? z.Ladegrenze : (double?)null,
                    LadeprioPv = z.Ladeprio_PV,
                    // -1 heisst "nicht gesetzt" (in der Datenbank NULL) - in der
                    // Komponente ebenfalls null. 0 ist dagegen GUELTIG ("ganz unten").
                    Anschlusshoehe = (z.Anschlusshoehe >= 0 && z.Anschlusshoehe <= 1)
                        ? z.Anschlusshoehe : (double?)null
                });
            }
            return l;
        }

        /// <summary>Die Gegenrichtung: aus den Zeilen der Komponente wieder Modelle.</summary>
        private static List<Z_AnlageSenkeModel> Modelle(
            int idAnlage, IReadOnlyList<SenkenzeileDaten> zeilen)
        {
            var l = new List<Z_AnlageSenkeModel>();
            if (zeilen == null) return l;

            for (int i = 0; i < zeilen.Count; i++)
            {
                SenkenzeileDaten z = zeilen[i];
                l.Add(new Z_AnlageSenkeModel
                {
                    ID_Anlage = idAnlage,
                    Rang = i + 1,
                    Ziel = z.Ziel,
                    ID_Puffer = z.IdPuffer,
                    Bedarfsart = z.Bedarfsart,
                    Ladeprio = z.Ladeprio,
                    Ladegrenze = z.Ladegrenze ?? 0,
                    Ladeprio_PV = z.LadeprioPv,
                    Anschlusshoehe = z.Anschlusshoehe ?? -1
                });
            }
            return l;
        }

        /// <summary>
        /// Schreibt die Senkenliste UND die Verbundmitglieder
        /// (<c>ListeSpeichern</c>:2018-2034). Die Mitgliederliste geht IMMER heraus,
        /// auch leer — das ist der Weg, auf dem ein Verbund wieder aufgelöst wird.
        /// </summary>
        private static bool Schreiben(int idAnlage, IReadOnlyList<SenkenzeileDaten> zeilen,
                                      IReadOnlyList<int> verbund)
        {
            if (idAnlage <= 0) return false;

            bool ok = new Z_AnlageSenkeCtrl().SchreibenJeAnlage(idAnlage, Modelle(idAnlage, zeilen));
            if (!AnlagePufferVerbundCtrl.Schreiben(idAnlage, new List<int>(verbund))) ok = false;
            return ok;
        }

        /// <summary>
        /// Die Projektpuffer als Auswahlzeilen — mit der Bitmaske ihres Klassen-Sets,
        /// nach der die Gruppenköpfe entstehen (<c>FuelleCombo</c>:1527-1557).
        /// </summary>
        private static IReadOnlyList<SenkenPuffer> Pufferliste(int idProjekt, string verwendung)
        {
            var l = new List<SenkenPuffer>();
            Dictionary<int, PufferSpCtrl.KlassenSet> sets =
                PufferSpCtrl.KlassenSetsJeProjekt(idProjekt);

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(idProjekt, verwendung))
            {
                PufferSpCtrl.KlassenSet set;
                if (!sets.TryGetValue(p.ID, out set) || set == null)
                    set = new PufferSpCtrl.KlassenSet(true, false, false);

                int maske = (set.Heizung ? 1 : 0) + (set.Brauchwasser ? 2 : 0) +
                            (set.Prozess ? 4 : 0);

                l.Add(new SenkenPuffer(p.ID, p.Bezeichner, maske,
                                       Warnkriterien.KlassenSetAnzeige(set)));
            }
            return l;
        }

        /// <summary>
        /// „Lädt als n. von m … bis x %" (<c>PositionsText</c>:1753-1775). Bezugsgröße
        /// ist bei Rang 1 der LEITSPEICHER und damit der Verbund als Ganzes.
        /// </summary>
        private static string Position(int idProjekt, int idAnlage, int idType,
                                       SenkenzeileDaten z, bool zweitsenke)
        {
            if (z == null || z.IdPuffer <= 0) return "";

            double grenze = z.Ladegrenze ?? 0;
            if (grenze < 0) grenze = 0;

            List<Ladeordnung.LadeEintrag> vorschau = Ladeordnung.LadereihenfolgeVorschau(
                idProjekt, z.IdPuffer, idAnlage, idType, zweitsenke,
                z.Ladeprio, grenze, z.LadeprioPv);

            int pos = Ladeordnung.Position(vorschau, idAnlage, zweitsenke);
            if (pos <= 0) return "";

            string text = string.Format(MyResource.Resource.SIM_POSITION_LAEDT_ALS,
                                        pos, vorschau.Count);
            if (vorschau.Count > 0 && pos <= vorschau.Count)
                text += Environment.NewLine +
                        string.Format(MyResource.Resource.SIM_POSITION_BIS,
                                      vorschau[pos - 1].Obergrenze.ToString("0.#"));
            return text;
        }

        /// <summary>
        /// Die Verwendung, die der Absprung „Pufferspeicher anlegen…" je Senkenziel
        /// mitgibt (<c>btnPufferAnlegen_Click</c>:1799-1804). Für das S1-Ziel
        /// <c>PufferProzess</c> gibt es keinen Altwert; dort bleibt es bei der
        /// Heizungs-Vorbelegung, den Kanal stellt das Klassen-Set ein (Konzept 6.1).
        /// </summary>
        private static string VerwendungZuZiel(string ziel)
        {
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                return WaermesenkeClass.VERWENDUNG_KOMBI;
            if (string.Equals(ziel, DbWerte.WS_ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                return WaermesenkeClass.VERWENDUNG_BRAUCHWASSER;
            return WaermesenkeClass.VERWENDUNG_HEIZUNG;
        }
    }
}
