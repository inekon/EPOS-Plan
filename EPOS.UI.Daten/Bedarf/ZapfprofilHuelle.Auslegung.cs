using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Hülle der Überlagerung „Auslegung"</b> (Umsetzungskonzept Zapfprofilgenerator 4.5,
    /// 4.7, 5.1, 5.5; Stufe Z2, Gruppe 2) — plattformfrei, die zweite Hälfte von
    /// <see cref="ZapfprofilHuelle"/>.
    ///
    /// <para><b>Die Auslegung ruft den Kern.</b> <see cref="Auslegung(int, ZapfprofilEingabeDaten, ZapfprofilAuslegungEingabeDaten, ZapfprofilStand, ZapfprofilStufe)"/>
    /// bildet aus dem Arbeitsstand des Dialogs und den Eingaben der Überlagerung den Stand des
    /// Kerns (<see cref="AlsStand"/>) und rechnet ihn über
    /// <see cref="ZapfprofilCtrl.Auslegung"/> — denselben Eingang wie die Bilanz, dazu
    /// Bedarfstage, DIN-4708-Katalog, Nenninhalte und die Laufangaben. Die Bilder baut
    /// <see cref="ZapfprofilBilder"/> aus den Ergebnissen des Kerns; die Hülle rechnet keinen
    /// Bedarf und keine Kurve.</para>
    ///
    /// <para><b>Benannt statt still.</b> Ablehnungen des Kerns (<see cref="ZapfAuslegungException"/>,
    /// <see cref="ZapfprofilEingabeException"/>, <see cref="ParametersatzException"/>) kommen als
    /// <see cref="ZapfprofilMeldung"/> mit dem Satz des Kerns (<see cref="ZapfSatz"/>) in der
    /// Oberflächensprache; die Warnliste trägt je Kennung des Kerns einen Titel in der
    /// Oberflächensprache (<c>ZPG_AUSHINW_…</c>) und den Satz des Kerns mit Zahlen in der Kultur
    /// der Oberfläche (N11 (k)).</para>
    ///
    /// <para><b>Der Punkt ist Ergebnis, nicht Eingabe.</b> Der mit OK übernommene Punkt geht nur in
    /// Übernahme und Speichern; der Rechenweg der Überlagerung rechnet ohne ihn
    /// (<see cref="OhnePunkt"/>) — sonst steuerte ein alter Punkt Großanlagenerkennung,
    /// Speichertemperatur und Warnliste und damit den neuen Punkt. Ändert der Anwender danach eine
    /// Zone, ist der Punkt überholt (<see cref="ZapfprofilEingabeDaten.PunktUeberholt"/>) und wird
    /// beim Speichern verworfen.</para>
    ///
    /// <para><b>Geschrieben wird nicht hier.</b> OK der Überlagerung legt die Eingaben samt Punkt
    /// in den Arbeitsstand des Dialogs (<see cref="ZapfprofilEingabeDaten.Auslegung"/>); das OK
    /// des Bedarfsprofil-Dialogs schreibt sie im gemeinsamen Vorgang über
    /// <see cref="AuslegungSpeichern"/> — Projektgrößen in <c>Tab_TwwProjekt</c>, ein
    /// konstruierter Bedarfstag als Katalogzeile. Erzeugerart, Werkstoff, Personen und Bezug des
    /// Füllstands stehen ab Schritt 124 als Projektgrößen darin; der Vorschlag des
    /// Anlagenbestands bleibt ein Vorschlag (N10 (i)).</para>
    /// </summary>
    internal static partial class ZapfprofilHuelle
    {
        /// <summary>Die Kennungen der Warnliste, die der Rechenweg der Auslegung vergibt — je eine Ressource <c>ZPG_AUSHINW_…</c>.</summary>
        internal static readonly string[] AUSLEGUNGSHINWEISE =
        {
            "KONSTRUKTOR_OEFFNEN", "BEDARFSTAG_NICHT_RECHENBAR", Bedarfstag.VERMERK_SPITZEN_UNTERSCHAETZT,
            "ZIRKULATION_NICHT_RECHENBAR", "SPEICHERAUSLEGUNG_NICHT_RECHENBAR", "GROSSANLAGE_NICHT_RECHENBAR",
            Grossanlage.HINWEIS_GROSSANLAGE, Grossanlage.HINWEIS_OHNE_ZIRKULATION, ZapfprofilAuslegung.HINWEIS_TEMPERATUR_GROSSANLAGE,
            "SPEICHERTEMPERATUR_UNTER_MINDEST", "SPEICHERVERLUST_NULL", "SUMMENLINIE_NICHT_MONOTON", "UEBERTRAGER_UNPLAUSIBEL",
            "WERTEPAARKURVE", "WOHNUNGSSTATION_JE_EINHEIT", Dreiergruppe.HINWEIS_REIHENFOLGE, Din4708Kennzahl.HINWEIS_WAERMEPUMPE,
            Din4708Kennzahl.HINWEIS_TEILGUELTIG, "GUELTIGKEIT_DIN_GLF", TwwSpeicherauslegung.HINWEIS_GLF_WANNEN,
            "GLF_GUELTIGKEITSGRENZE", "LADELEISTUNG_ZU_KLEIN", "MASSGEBEND_WOCHENENDE", "DEFIZIT_WAECHST",
            TwwSpeicherauslegung.DMAX_NULL, "SUMMENKONTROLLE", "SUMMENLINIE_AUSSERHALB_BAND", "KLASSISCH_WEIT_UEBER_BAND",
            "MEHRSPEICHER", "NENNINHALTE_FEHLEN", ZapfprofilCtrl.HINWEIS_NENNINHALTE_EINSTELLUNG,
            ZapfprofilCtrl.HINWEIS_NENNINHALTE_PARAMETER, "NL_KRITERIUM", ZapfHinweis.PARAMETER_FEHLT,
            "PERZENTIL_NICHT_BELASTBAR", "KONSISTENZ_STOCHASTISCHE_SPITZE", "WURZEL_N_VERGLEICH", "STOCHASTIK_NICHT_RECHENBAR"
        };

        // =================================================================================
        // Parametersatz der Überlagerung
        // =================================================================================

        /// <summary>
        /// Der Parametersatz der Komponente <c>ZapfprofilAuslegungDialog.razor</c> zum
        /// Arbeitsstand <paramref name="eingabe"/> des Zapfprofil-Dialogs: <c>Daten</c>
        /// (<see cref="ZapfprofilAuslegungStartDaten"/>), <c>Texte</c>, <c>Rechnen</c>,
        /// <c>StochastischRechnen</c>, <c>Konstruieren</c>, <c>HilfeSchluessel</c>,
        /// <c>HilfeRechenweg</c>. Die Delegaten rechnen gegen die Zonen, mit denen die Überlagerung
        /// öffnete. <c>Rechnen</c> rechnet im Zeichenlauf und darum immer deterministisch — das
        /// Ensemble des Bedarfstags zieht allein <c>StochastischRechnen</c>, nebenläufig auf einem
        /// Arbeitsfaden mit Abbruchmarke (5.1).
        /// </summary>
        internal static IReadOnlyDictionary<string, object> AuslegungGaben(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                           ZapfprofilStand basis, ZapfprofilStufe stufe,
                                                                           bool stochastisch = false)
        {
            ZapfprofilEingabeDaten zonen = (eingabe ?? new ZapfprofilEingabeDaten()).Kopie();
            return new Dictionary<string, object>
            {
                ["Daten"] = AuslegungStart(idProjekt, zonen, basis, stufe, stochastisch),
                ["Texte"] = AuslegungTexte(),
                ["Rechnen"] = new Func<ZapfprofilAuslegungEingabeDaten, ZapfprofilAuslegungDaten>(
                    a => Auslegung(idProjekt, zonen, Deterministisch(a), basis, stufe)),
                ["StochastischRechnen"] = new Func<ZapfprofilAuslegungEingabeDaten, CancellationToken, Task<ZapfprofilAuslegungDaten>>(
                    (a, abbruch) => Kulturweitergabe.Starten(() => Auslegung(idProjekt, zonen, a, basis, stufe, abbruch), abbruch)),
                ["Konstruieren"] = new Func<IReadOnlyList<ZapfprofilKonstruktorZeileDaten>, string, double?, int?, ZapfprofilKonstruktorErgebnis>(
                    BedarfstagKonstruieren),
                ["HilfeSchluessel"] = HILFE_DIALOG,
                ["HilfeRechenweg"] = HILFE_AUSLEGUNG_RECHENWEG
            };
        }

        /// <summary>
        /// Der Stand der Überlagerung beim Öffnen: die Eingaben (die des Arbeitsstands, sonst aus
        /// den Projektgrößen), die Bedarfstage des Katalogs, die Zapfregeln samt Namensvorschlag,
        /// die Verfügbarkeit und das erste Ergebnis. „Stochastisch rechnen" ist eine Laufangabe:
        /// Sie kommt allein aus <paramref name="stochastisch"/> — „Auslegung…" öffnet mit dem
        /// Schalter aus, der Fußknopf „Stochastisch rechnen" mit ihm an; ein Schalter aus einem
        /// früheren OK gilt nicht mehr. Mit Schalter gibt es kein erstes Ergebnis — die Überlagerung
        /// rechnet gleich EINEN nebenläufigen Lauf samt Ensemble, keinen deterministischen davor.
        /// </summary>
        internal static ZapfprofilAuslegungStartDaten AuslegungStart(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                                     ZapfprofilStand basis, ZapfprofilStufe stufe,
                                                                     bool stochastisch = false)
        {
            eingabe ??= new ZapfprofilEingabeDaten();
            ZapfprofilAuslegungEingabeDaten anfang = eingabe.Auslegung?.Kopie() ?? AuslegungAusStand(basis);
            // Ladeleistung, Ladefenster und Speichertemperatur führt auch der Dialog (Stufen Erweitert
            // und Experte, Z4): Die Überlagerung beginnt mit seinem Stand.
            eingabe.Gebaeude?.InAuslegung(anfang);
            anfang.Stochastisch = stochastisch;
            var start = new ZapfprofilAuslegungStartDaten
            {
                Stufe = stufe,
                Eingabe = anfang,
                Kontext = Format(Text_("ZPG_AUS_KONTEXT", "Summe aller Zonen · {0} Zonen · Stufe {1}"),
                                 eingabe.Zonen.Count.ToString(CultureInfo.CurrentCulture), Stufenname(stufe))
            };

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            start.Verfuegbar = verfuegbar.Ja;
            start.Sperrgrund = verfuegbar.Ja ? "" : Verfuegbarkeitsgrund(verfuegbar);
            if (!verfuegbar.Ja) return start;

            start.Bedarfstage = ZapfprofilCtrl.Bedarfstage().Select(t => AlsBedarfstag(t, false)).ToList();
            StochastikRahmen(start);
            // Die Wertemengen des Schemas (Schritt 124): Bezugsart eines konstruierten Tags, Bezug des Füllstands.
            start.Bezugsarten = TwwSchema.Werte(TwwSchema.BEZUGSART_WERTE)
                .Select(b => new ZapfprofilKatalogeintragDaten { Id = b, Name = Bezugsgroesse((ZapfBezugsart)b) }).ToList();
            start.Fuellstandbezuege = TwwSchema.Werte(TwwSchema.FUELLSTAND_BEZUG_WERTE)
                .Select(b => new ZapfprofilKatalogeintragDaten
                {
                    Id = b,
                    Name = Satztext(TwwSpeicherauslegung.Fuellstandbegriff((ZapfFuellstandbezug)b))
                }).ToList();
            try
            {
                Parametersatz ps = ZapfprofilCtrl.Parameter();
                start.NameVorschlag = ZapfprofilCtrl.FreierBedarfstagname(Text_("ZPG_AUS_KON_NAME_STAMM", "Eigener Bedarfstag"),
                                                                           ps.Katalogversion);
                try
                {
                    start.Regeln = ZapfprofilCtrl.Konstruktorregeln(ps)
                        .Select(r => new ZapfprofilRegelDaten(r.Name, r.VolumenstromLJeMin, r.DauerMin, r.ZapftemperaturC)).ToList();
                }
                catch (Exception ex) when (ex is ZapfAuslegungException || ex is ParametersatzException)
                {
                    start.RegelnGrund = Format(Text_("ZPG_AUS_KON_REGELN_UNGUELTIG",
                        "Die Zapfregeln des Katalogs sind ungültig — nur Zeilen mit direktem Volumen: {0}"), Ausnahmetext(ex));
                }
            }
            catch (ParametersatzException) { /* das Ergebnis nennt den Grund */ }

            // Das erste Ergebnis rechnet im Zeichenlauf — deterministisch; mit Schalter keines: Das
            // Ensemble zieht die Überlagerung gleich nebenläufig (StochastischRechnen), ein Lauf.
            if (!stochastisch) start.Ergebnis = Auslegung(idProjekt, eingabe, start.Eingabe, basis, stufe);
            return start;
        }

        /// <summary>
        /// Die Stochastik der Überlagerung (4.4, 4.5 b; Stufe Z3): Wertemenge und Vorgabe des
        /// Perzentils (Schema, DDL), die Grenzen der Realisierungen des Bedarfstags (Schema, Kern)
        /// und je Perzentil die Mindestzahl ⌈1/(1 − p)⌉ und die Vorgabe ⌈Vielfaches · 1/(1 − p)⌉
        /// aus dem Kern; ist die Vorgabe nicht bestimmbar, steht der Grund daneben.
        /// </summary>
        private static void StochastikRahmen(ZapfprofilAuslegungStartDaten start)
        {
            start.Perzentile = TwwSchema.Perzentile.ToList();
            start.PerzentilVorgabe = ZapfprofilCtrl.ProjektVorgabe()?.Perzentil;
            start.RealisierungenMindestens = TwwSchema.RealisierungenMindestens;
            start.RealisierungenHoechstens = Zapfensemble.HOECHSTENS;
            foreach (int p in TwwSchema.Perzentile) start.Mindestzahl[p] = Zapfensemble.Mindestzahl(p);
            try
            {
                Parametersatz ps = ZapfprofilCtrl.Parameter();
                foreach (int p in TwwSchema.Perzentile)
                    start.RealisierungenVorgabe[p] = Zapfensemble.RealisierungenAuslegung(null, p, ps);
            }
            catch (ParametersatzException ex) { OhneVorgabe(start, Satztext(ex.Satz)); }
            catch (ZapfAuslegungException ex) { OhneVorgabe(start, Satztext(ex.Satz)); }
            catch (ZapfprofilEingabeException ex) { OhneVorgabe(start, Satztext(ex.Satz)); }
        }

        private static void OhneVorgabe(ZapfprofilAuslegungStartDaten start, string grund)
        {
            start.RealisierungenVorgabe.Clear();
            start.RealisierungenVorgabeGrund = grund ?? "";
        }

        // =================================================================================
        // Auslegung
        // =================================================================================

        /// <summary>Die Auslegung zum gespeicherten Stand des Projekts (Stufe Einfach).</summary>
        internal static ZapfprofilAuslegungDaten Auslegung(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                           ZapfprofilAuslegungEingabeDaten auslegung)
            => Auslegung(idProjekt, eingabe, auslegung, ZapfprofilCtrl.Lies(idProjekt), ZapfprofilStufe.Einfach);

        /// <summary>
        /// <b>Die Auslegung</b> zu den Zonen <paramref name="eingabe"/> und den Eingaben
        /// <paramref name="auslegung"/> — über <see cref="ZapfprofilCtrl.Auslegung"/>, immer auf dem
        /// Generatorweg. Ohne Zone keine Rechnung; fehlen Tabellen, Katalogversion oder Kalender,
        /// der benannte Grund.
        /// </summary>
        internal static ZapfprofilAuslegungDaten Auslegung(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                           ZapfprofilAuslegungEingabeDaten auslegung, ZapfprofilStand basis,
                                                           ZapfprofilStufe stufe)
            => Auslegung(idProjekt, eingabe, auslegung, basis, stufe, CancellationToken.None);

        /// <summary>
        /// Dieselbe Auslegung mit Abbruchmarke — der nebenläufige Lauf „Stochastisch rechnen" der
        /// Überlagerung (5.1): Die Marke beendet die Ziehung des Ensembles mit
        /// <see cref="OperationCanceledException"/>; jede andere Ablehnung kommt benannt zurück.
        /// </summary>
        internal static ZapfprofilAuslegungDaten Auslegung(int idProjekt, ZapfprofilEingabeDaten eingabe,
                                                           ZapfprofilAuslegungEingabeDaten auslegung, ZapfprofilStand basis,
                                                           ZapfprofilStufe stufe, CancellationToken abbruch)
        {
            if (eingabe == null || eingabe.Zonen.Count == 0)
                return OhneAuslegung(ZapfprofilAuslegungZustand.NichtGerechnet, "ZPG_AUS_MSG_KEINE_ZONE",
                    Text_("ZPG_AUS_MSG_KEINE_ZONE", "Es ist keine Zone angelegt — ohne Zone keine Auslegung."), "");

            ZapfVerfuegbarkeit verfuegbar = ZapfprofilCtrl.Verfuegbar();
            if (!verfuegbar.Ja)
                return OhneAuslegung(ZapfprofilAuslegungZustand.Abgebrochen, VerfuegbarkeitsKennung(verfuegbar),
                                     Verfuegbarkeitsgrund(verfuegbar), verfuegbar.Klartext);
            if (!ZapfprofilCtrl.KalenderLesen(idProjekt, out int jan1, out bool[] we))
                return OhneAuslegung(ZapfprofilAuslegungZustand.Abgebrochen, "ZPG_MSG_KEINE_KLIMAREGION",
                    Text_("ZPG_MSG_KEINE_KLIMAREGION", "Das Projekt hat keine Klimaregion — ohne Kalender keine Vorschau."), "");

            auslegung ??= AuslegungAusStand(basis);
            ZapfprofilEingabeDaten mit = eingabe.Kopie();
            mit.Auslegung = auslegung;
            // In der Überlagerung gelten ihre Eingaben: Die geteilten gebäudeweiten Größen (Ladeleistung,
            // Ladefenster, Speichertemperatur) nehmen den Stand der Überlagerung an, die übrigen bleiben.
            mit.Gebaeude?.AusAuslegung(auslegung);
            Auslegungsrechnung r;
            ProjektStand rechenprojekt;
            try
            {
                // Der übernommene Punkt (Arbeitsstand oder gespeichert) bleibt draußen: Er ist das
                // Ergebnis dieser Rechnung, nicht ihre Eingabe. „Stochastisch rechnen" ist eine
                // Laufangabe; Seed, Perzentil und Realisierungen stehen in den Projektgrößen.
                ZapfprofilStand stand = AlsStand(mit, basis);
                stand = stand with { Weg = BrauchwasserWeg.Generator, Projekt = OhnePunkt(stand.Projekt) };
                rechenprojekt = stand.Projekt;
                r = ZapfprofilCtrl.Auslegung(idProjekt, stand, jan1, we,
                    new Auslegungslauf(AlsErzeugerart(auslegung.Erzeugerart), AlsWerkstoff(auslegung.Werkstoff),
                                       auslegung.Stochastisch, abbruch, (ZapfStufe)(int)stufe));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is ZapfprofilEingabeException || ex is ParametersatzException || ex is ZapfAuslegungException)
            {
                ZapfSatz satz = SatzAus(ex);
                return OhneAuslegung(ZapfprofilAuslegungZustand.Abgebrochen, Satzkennung(satz, "ZPG_AUS_MSG_UNERWARTET"),
                                     Satztext(satz), ex.Message);
            }
            catch (Exception ex)
            {
                return OhneAuslegung(ZapfprofilAuslegungZustand.Abgebrochen, "ZPG_AUS_MSG_UNERWARTET",
                    Format(Text_("ZPG_AUS_MSG_UNERWARTET", "Die Auslegung konnte nicht gerechnet werden: {0}"), ex.Message), ex.Message);
            }

            try
            {
                ZapfprofilAuslegungDaten d = AlsAuslegung(r, stufe, auslegung);
                if (d.Stochastisch)
                {
                    // Derselbe Seed und dasselbe Perzentil wie im Eingang der Rechnung.
                    ProjektStand p = rechenprojekt ?? ZapfprofilCtrl.ProjektVorgabe();
                    d.Status = Format(Text_("ZPG_AUS_STATUS_GERECHNET_STOCHASTISCH",
                                            "Auslegung gerechnet · stochastisch · Perzentil P{0} · Seed {1}"),
                                      p?.Perzentil ?? 0, p?.Seed ?? 0);
                }
                return d;
            }
            catch (Exception ex)
            {
                return OhneAuslegung(ZapfprofilAuslegungZustand.Abgebrochen, "ZPG_AUS_MSG_UNERWARTET",
                    Format(Text_("ZPG_AUS_MSG_UNERWARTET", "Die Auslegung konnte nicht gerechnet werden: {0}"), ex.Message), ex.Message);
            }
        }

        /// <summary>Die Eingaben ohne „Stochastisch rechnen" — der Weg im Zeichenlauf zieht nie ein Ensemble.</summary>
        internal static ZapfprofilAuslegungEingabeDaten Deterministisch(ZapfprofilAuslegungEingabeDaten a)
        {
            if (a == null || !a.Stochastisch) return a;
            ZapfprofilAuslegungEingabeDaten d = a.Kopie();
            d.Stochastisch = false;
            return d;
        }

        private static ZapfprofilAuslegungDaten OhneAuslegung(ZapfprofilAuslegungZustand zustand, string kennung, string grund,
                                                             string klartext)
        {
            var d = new ZapfprofilAuslegungDaten
            {
                Zustand = zustand,
                Grund = grund ?? "",
                Status = Format(Text_("ZPG_AUS_STATUS_OHNE", "Keine Auslegung — {0}"), grund ?? "")
            };
            d.Meldungen.Add(new ZapfprofilMeldung(kennung, "", grund ?? "",
                zustand == ZapfprofilAuslegungZustand.Abgebrochen ? ZapfprofilMeldungsart.Fehler : ZapfprofilMeldungsart.Hinweis,
                klartext ?? ""));
            return d;
        }

        /// <summary>Das Ergebnis des Kerns als DTO: je Topologiegruppe Dreiergruppe, Empfehlung, Bilder, Vergleich und Warnliste.</summary>
        internal static ZapfprofilAuslegungDaten AlsAuslegung(Auslegungsrechnung r, ZapfprofilStufe stufe,
                                                             ZapfprofilAuslegungEingabeDaten eingabe)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            var d = new ZapfprofilAuslegungDaten
            {
                Zustand = ZapfprofilAuslegungZustand.Gerechnet,
                Stochastisch = eingabe?.Stochastisch == true,
                Status = Text_("ZPG_AUS_STATUS_GERECHNET", "Auslegung gerechnet · deterministisch · Perzentil erst mit „Stochastisch rechnen“"),
                ErzeugerartAngesetzt = AlsErzeugerart(r.Erzeugerart),
                // Der Vorschlag des Projekts (N10 (i)): der eindeutige Anlagenbestand — eine Wahl, die der
                // Anwender übernimmt; gespeichert wird sie erst mit ihr (Schritt 124).
                ErzeugerartVorschlag = AlsErzeugerart(r.Bestand?.Vorschlag)
            };

            if (eingabe != null && eingabe.Erzeugerart != ZapfprofilErzeugerart.KeineAngabe)
                d.ErzeugerartHerkunft = Text_("ZPG_AUS_ERZEUGERART_EINGABE", "Eingabe");
            else if (r.Bestand?.Vorschlag is ZapfErzeugerart vorschlag)
                d.ErzeugerartHerkunft = Format(Text_("ZPG_AUS_ERZEUGERART_BESTAND", "Vorschlag aus dem Anlagenbestand: {0}"),
                                               Erzeugerartname(vorschlag));
            else if (r.Bestand != null && r.Bestand.Mehrdeutig)
                d.ErzeugerartHerkunft = Text_("ZPG_AUS_ERZEUGERART_MEHRDEUTIG",
                                              "Der Anlagenbestand führt Kessel und Wärmepumpe — bitte wählen.");
            else
                d.ErzeugerartHerkunft = Text_("ZPG_AUS_ERZEUGERART_OHNE", "Der Anlagenbestand nennt keinen Erzeuger.");

            foreach (Auslegungsablehnung a in r.Ergebnis.Ablehnungen)
                d.Meldungen.Add(new ZapfprofilMeldung("ZPG_AUS_ZONE_ABGELEHNT", a.Zone ?? "",
                    MitZone(a.Zone, a.Satz, "ZPG_AUS_ZONE_ABGELEHNT", "Zone „{0}“ fehlt in der Auslegung: {1}"),
                    ZapfprofilMeldungsart.Ablehnung, a.Klartext ?? ""));
            foreach (Auslegungshinweis h in r.Ergebnis.Hinweise)
            {
                ZapfprofilWarnDaten w = Warnung(h);
                d.Meldungen.Add(new ZapfprofilMeldung(w.Kennung, "", w.Titel + ": " + w.Text,
                    h.Warnung ? ZapfprofilMeldungsart.Ablehnung : ZapfprofilMeldungsart.Hinweis, h.Text ?? ""));
            }

            foreach (Auslegungsgruppe g in r.Ergebnis.Gruppen) d.Gruppen.Add(AlsGruppe(g, stufe));

            // Die Karte "Herkunft" (N19): Die Ueberlagerung fuehrt keine eigene Stufe, deshalb
            // entscheidet die Huelle - ab Erweitert steht die Karte, in der Stufe Einfach nicht.
            d.HerkunftSichtbar = stufe != ZapfprofilStufe.Einfach;
            if (d.HerkunftSichtbar) d.Herkunft.AddRange(Herkunftszeilen(r.Ergebnis.Herkunft));
            return d;
        }

        /// <summary>Eine Topologiegruppe als DTO.</summary>
        internal static ZapfprofilAuslegungsgruppeDaten AlsGruppe(Auslegungsgruppe g, ZapfprofilStufe stufe)
        {
            bool speicher = g.Topologie == ZapfTopologie.Speicher;
            var d = new ZapfprofilAuslegungsgruppeDaten
            {
                Topologie = Text_("ZPG_AUS_TOPOLOGIE_" + Gross(g.Topologie.ToString()), g.Topologie.ToString()),
                Speicher = speicher,
                Zonen = g.Zonen.ToList(),
                Bedarfstag = g.Bedarfstag?.Bezeichner ?? "",
                BedarfstagWahl = Satztext(g.Bedarfstagwahl?.Grund),
                KonstruktorOeffnen = g.Bedarfstagwahl?.KonstruktorOeffnen == true && g.Bedarfstag == null,
                SpitzenUnterschaetzt = g.Bedarfstag?.SpitzenUnterschaetzt == true,
                SpeicherC = g.Speichertemperatur?.SpeicherC,
                SpeicherCHerkunft = g.Speichertemperatur == null ? "" : Speichertemperaturherkunft(g.Speichertemperatur.Quelle)
            };

            IReadOnlyList<Auslegungswert> dreier = g.Dreiergruppe;
            if (dreier.Count > 0) d.Hauptwert = Karte(dreier[0]);
            if (dreier.Count > 1) d.Perzentil = Karte(dreier[1]);
            if (dreier.Count > 2) d.Normvergleich = Karte(dreier[2]);
            d.PerzentilErgebnis = PerzentilDaten(g);

            Auslegungsempfehlung e = g.Empfehlung;
            if (e != null)
                d.Empfehlung = new ZapfprofilEmpfehlungDaten
                {
                    Rechenbar = e.Rechenbar,
                    Speicher = e.Verfahren == ZapfAuslegungsverfahren.Summenlinie,
                    VolumenL = e.VolumenL,
                    LeistungKw = e.LeistungKw,
                    NenninhaltL = e.NenninhaltL,
                    // Die Marke setzt der Kern je Stufe (N11 (c)); die Hülle rechnet sie nicht nach.
                    Schnellauslegung = e.Rechenbar && e.Schnellauslegung,
                    Vermerk = string.Join("; ", e.Vermerke.Select(Satztext)),
                    Grund = Satztext(e.Grund)
                };

            ZapfprofilAuslegungBildtexte bild = AuslegungBildtexte();
            Summenlinienergebnis sl = g.Summenlinie;
            if (sl != null)
            {
                d.LadezeitH = sl.Punkt?.LadezeitH;
                d.ZeitkonstanteMin = sl.ZeitkonstanteMin;
                d.Wertepaare = sl.Wertepaare.Count;
                d.Vermerk = Satztext(sl.Vermerk);
                Summenlinienkurven k = ZapfprofilBilder.SummenlinieKurven(g.Bedarfstag, sl.Nachweis);
                if (k != null) d.SummenlinieModell = ZapfprofilBilder.SummenlinieModell(k.BedarfKwh, k.VersorgungKwh, k.BeruehrungMinute, bild);
                if (sl.Wertepaare.Count >= 2)
                    d.WertepaarModell = ZapfprofilBilder.WertepaarkurveModell(
                        sl.Wertepaare.Select(p => p.LeistungKw).ToArray(), sl.Wertepaare.Select(p => p.VolumenL).ToArray(),
                        sl.Punkt?.LeistungKw, sl.Punkt?.VolumenL, bild);
            }

            Din4708Ergebnis din = g.Normvergleich;
            if (din != null)
            {
                d.KennzahlN = din.Gueltig ? din.KennzahlN : null;
                d.ZonenAusserhalb = din.ZonenAusserhalb.ToList();
                d.HinweisWaermepumpe = din.Hinweise.Any(h => h.Code == Din4708Kennzahl.HINWEIS_WAERMEPUMPE);
            }

            if (g.Speicherauslegung != null) d.Vergleich = Vergleich(g.Speicherauslegung, g.Woche, din, bild);

            foreach (Auslegungshinweis h in g.Hinweise) d.Warnliste.Add(Warnung(h));
            return d;
        }

        /// <summary>Die Kennung des Konsistenzhinweises der Speichergruppe (4.7, N11 (f)).</summary>
        internal const string HINWEIS_KONSISTENZ = "KONSISTENZ_STOCHASTISCHE_SPITZE";

        /// <summary>
        /// <b>Das Perzentil einer Gruppe als DTO</b> (4.5 b): bei Speicher die erforderlichen
        /// Volumina beim Φ_N des Summenlinienpunkts, sonst die Minutenspitze; das Streuband P50 …
        /// P99 samt Spannweite, die Gleichzeitigkeit als Ergebnis (GLF_V bzw. GLF_P) mit Σ n_E,
        /// die Belastbarkeit gegen die Mindestzahl ⌈1/(1 − p)⌉ und — nur Speicher — der
        /// Konsistenzhinweis als WERTE des Kerns: geprüft, solange der Parametersatz die Schwelle
        /// trägt (sonst nennt die Warnliste den fehlenden Parameter), mit Stundenspitze P_p,
        /// Schwelle und Φ_N — den Satz baut die Oberfläche in ihrer Sprache (N11 (k)). <c>null</c>
        /// ohne Lauf „Stochastisch rechnen" oder wenn das Ensemble nicht rechenbar war. Die Hülle
        /// rechnet nichts nach: jede Zahl kommt aus dem Kern.
        /// </summary>
        internal static ZapfprofilPerzentilDaten PerzentilDaten(Auslegungsgruppe g)
        {
            Perzentilergebnis p = g?.Perzentil;
            if (p == null) return null;
            bool speicher = g.Topologie == ZapfTopologie.Speicher;
            bool volumen = speicher && p.VolumenL != null;
            Perzentilwerte w = volumen ? p.VolumenL : p.MinutenspitzeKw;
            var d = new ZapfprofilPerzentilDaten
            {
                Perzentil = p.Perzentil,
                Seed = p.Seed,
                Realisierungen = p.Realisierungen,
                Mindestzahl = Zapfensemble.Mindestzahl(p.Perzentil),
                Belastbar = p.Belastbar,
                Tag = p.Tag,
                Datum = Datum(p.Tag),
                Volumen = volumen,
                LeistungKw = volumen ? p.LeistungKw : null,
                Minimum = w.Minimum,
                Maximum = w.Maximum,
                OhneNachweis = p.OhneNachweis,
                MinutenspitzeKw = p.MinutenspitzeKw.Wert(p.Perzentil),
                StundenspitzeKw = p.StundenspitzeKw.Wert(p.Perzentil),
                Gleichzeitigkeit = volumen ? p.GleichzeitigkeitVolumen : p.GleichzeitigkeitLeistung,
                Einheiten = p.Zonen.Sum(z => z.Einheiten),
                WurzelNKw = p.WurzelNSchaetzungKw,
                SpitzeJeEinheitKw = p.SpitzeJeEinheitKw,
                SpitzeJeEinheitZone = p.SpitzeJeEinheitZone ?? ""
            };
            foreach (int stufe in Perzentilwerte.Stufen)
                d.Streuband.Add(new ZapfprofilPerzentilZeileDaten(stufe, w.Wert(stufe)));
            if (speicher && p.KonsistenzSchwelle.HasValue)
            {
                d.KonsistenzGeprueft = true;
                d.KonsistenzAuffaellig = p.KonsistenzAuffaellig;
                d.KonsistenzSpitzeKw = p.KonsistenzSpitzeKw;
                d.KonsistenzSchwelle = p.KonsistenzSchwelle;
                d.KonsistenzLeistungKw = p.LeistungKw;
            }
            return d;
        }

        /// <summary>
        /// Ein Jahrestag 1 … 365 als Datum in der Oberflächensprache („17. Januar", „January 17") —
        /// über das Rechenjahr ohne Schaltjahr (<see cref="Zapfkalender"/>), nie über ein
        /// Kalenderjahr; außerhalb des Rasters leer.
        /// </summary>
        internal static string Datum(int jahrestag)
        {
            if (jahrestag < 1 || jahrestag > Zapfkalender.TAGE) return "";
            (int tag, int monat) = TagUndMonat(jahrestag);
            return Format(Text_("ZPG_AUS_DATUM", "{0}. {1}"), tag.ToString(CultureInfo.CurrentCulture), Monatsname(monat));
        }

        private static ZapfprofilKarteDaten Karte(Auslegungswert w) => new ZapfprofilKarteDaten
        {
            Stand = (ZapfprofilKartenstand)(int)w.Status,
            VolumenL = w.VolumenL,
            LeistungKw = w.LeistungKw,
            Empfohlen = w.Empfohlen,
            Text = AblehnungsSatz(w.Ablehnung) ?? Satztext(w.Satz)
        };

        /// <summary>
        /// Der Satz einer benannten Ablehnung des Rechenwegs in der Oberflächensprache — mit der Zone
        /// davor, wo sie eine trägt und der Satz sie nicht schon nennt; <c>null</c> ohne Ablehnung
        /// oder ohne Satz.
        /// </summary>
        private static string AblehnungsSatz(ZapfAblehnung a)
            => a?.Satz == null ? null : MitZone(a.Zone, a.Satz, "ZPG_MSG_ZONE", "Zone „{0}“: {1}");

        /// <summary>Der Verfahrensvergleich nach V4 als DTO samt Wochenbild; der größte Wert im Band ist markiert.</summary>
        private static ZapfprofilVergleichDaten Vergleich(Speicherauslegungsergebnis sa, Wochenreihe woche, Din4708Ergebnis din,
                                                          ZapfprofilAuslegungBildtexte bild)
        {
            var v = new ZapfprofilVergleichDaten
            {
                BandMinL = sa.BandMinL,
                BandMaxL = sa.BandMaxL,
                NenninhaltL = sa.NenninhaltL,
                Mehrspeicher = sa.Mehrspeicher,
                KennzahlN = din != null && din.Gueltig ? din.KennzahlN : null,
                LadeleistungKw = sa.Ladeleistung.Angesetzt,
                LadeManuell = sa.Ladeleistung.IstManuell,
                LadeRechenweg = Satztext(sa.LadeRechenweg),
                Personen = sa.Personen,
                Nutzanteil = sa.Nutzanteil,
                Zuschlag = sa.Zuschlag,
                DmaxKwh = sa.DmaxKwh,
                ProfilbasiertVorhanden = sa.ProfilbasiertVorhanden,
                FuellstandBezugL = sa.FuellstandBezugL,
                FuellstandBezug = sa.FuellstandBezug.HasValue ? Satztext(TwwSpeicherauslegung.Fuellstandbegriff(sa.FuellstandBezug.Value)) : "",
                FuellstandBezugArt = sa.FuellstandBezug.HasValue ? (ZapfprofilFuellstandbezug)(int)sa.FuellstandBezug.Value
                                                                 : ZapfprofilFuellstandbezug.Vorgabe,
                Ladeleistung = AlsSchaetzhilfe(sa.LadeSchaetzhilfe),
                PersonenVorschlag = sa.PersonenWert.HasValue && !double.IsNaN(sa.PersonenWert.Value.Vorschlag)
                    ? sa.PersonenWert.Value.Vorschlag : (double?)null,
                PersonenManuell = sa.PersonenWert.HasValue && sa.PersonenWert.Value.IstManuell,
                KapazitaetKwh = sa.KapazitaetKwh,
                MinFuellstandKwh = sa.MinFuellstandKwh,
                ReserveAnteil = sa.ReserveAnteil
            };
            foreach (Verfahrensvolumen z in sa.Verfahren)
                v.Verfahren.Add(new ZapfprofilVerfahrenDaten
                {
                    Verfahren = Text_("ZPG_AUS_VERFAHREN_" + Gross(z.Verfahren.ToString()), z.Verfahren.ToString()),
                    VolumenL = z.VolumenL,
                    Gueltig = z.Gueltig,
                    ImBand = z.ImBand,
                    Groesster = z.ImBand && z.VolumenL.HasValue && sa.BandMaxL.HasValue && z.VolumenL.Value == sa.BandMaxL.Value,
                    Nachrichtlich = z.Verfahren == ZapfSpeicherverfahren.Klassisch,
                    Kennwert = Satztext(z.Kennwert),
                    Rechenweg = Satztext(z.Rechenweg)
                });

            if (sa.ProfilbasiertVorhanden && sa.StundeDesTags.HasValue && sa.TagInWoche2.HasValue && sa.Wochentag.HasValue)
                v.Zeitpunkt = Format(Text_("ZPG_AUS_ZEITPUNKT",
                    "Maßgebender Zeitpunkt der Stundenbilanz: {0}, Tag {1} von 14 (in Woche 2 gezählt), {2}–{3} Uhr."),
                    Wochentagsname(sa.Wochentag.Value),
                    (Wochenreihe.TAGE + sa.TagInWoche2.Value).ToString(CultureInfo.CurrentCulture),
                    sa.StundeDesTags.Value.ToString(CultureInfo.CurrentCulture),
                    (sa.StundeDesTags.Value + 1).ToString(CultureInfo.CurrentCulture));

            Wochenbildreihen w = ZapfprofilBilder.Wochenreihen(woche, sa);
            if (w != null)
                v.WochenModell = ZapfprofilBilder.AuslegungswocheModell(w.ZapfungKw, w.ZirkulationKw, w.LadungKw, w.DefizitKwh,
                                                                        w.FuellstandKwh, sa.FuellstandBezugL, w.MassgebendStunde, bild);
            return v;
        }

        /// <summary>
        /// Ein Hinweis des Kerns als Eintrag der Warnliste: Titel aus <c>ZPG_AUSHINW_…</c>; ein Befund
        /// der Bilanz, den die Auslegung mitträgt (Mengengerüst, Tagesgang, Zirkulation), trägt den
        /// Titel der Warnliste der Bilanz (<c>ZPG_WARN_…</c>) samt dessen Kennung; sonst „Hinweis".
        /// Satz des Kerns, Stufe nach der Warnlogik.
        /// </summary>
        internal static ZapfprofilWarnDaten Warnung(Auslegungshinweis h)
        {
            string kennung = AuslegungsHinweisSchluessel(h.Code);
            string titel = Text_(kennung, null);
            if (titel == null && Text_("ZPG_WARN_" + (h.Code ?? ""), null) is string bilanz)
            {
                kennung = "ZPG_WARN_" + h.Code;
                titel = bilanz;
            }
            titel ??= Text_("ZPG_AUS_HINWEIS", "Hinweis");
            return new ZapfprofilWarnDaten(kennung, titel, AblehnungsSatz(h.Ablehnung) ?? Satztext(h.Satz),
                                           h.Warnung ? ZapfprofilWarnstufe.Warnung : ZapfprofilWarnstufe.Hinweis);
        }

        /// <summary>Der Ressourcenschlüssel des Titels einer Hinweiskennung der Auslegung: <c>ZPG_AUSHINW_</c> + Kennung.</summary>
        internal static string AuslegungsHinweisSchluessel(string code) => "ZPG_AUSHINW_" + (code ?? "");


        private static string Speichertemperaturherkunft(Speichertemperaturquelle q)
        {
            switch (q)
            {
                case Speichertemperaturquelle.Projekt: return Text_("ZPG_AUS_SPEICHERC_PROJEKT", "Eingabe");
                case Speichertemperaturquelle.Grossanlage:
                    return Text_("ZPG_AUS_SPEICHERC_GROSSANLAGE", "Vorgabe · Großanlage nach DVGW W 551");
                case Speichertemperaturquelle.Schnellpfad:
                    return Text_("ZPG_AUS_SPEICHERC_SCHNELLPFAD", "Vorgabe · Vereinfachungsverfahren der A100");
                default: return Text_("ZPG_AUS_SPEICHERC_VORGABE", "Vorgabe des Katalogs");
            }
        }

        private static string Stufenname(ZapfprofilStufe stufe)
        {
            switch (stufe)
            {
                case ZapfprofilStufe.Erweitert: return Text_("ZPG_STUFE_ERWEITERT", "Erweitert");
                case ZapfprofilStufe.Experte: return Text_("ZPG_STUFE_EXPERTE", "Experte");
                default: return Text_("ZPG_STUFE_EINFACH", "Einfach");
            }
        }

        private static string Erzeugerartname(ZapfErzeugerart art)
            => art == ZapfErzeugerart.Waermepumpe ? Text_("ZPG_AUS_ERZEUGER_WAERMEPUMPE", "Wärmepumpe")
                                                  : Text_("ZPG_AUS_ERZEUGER_KESSEL", "Kessel");

        // =================================================================================
        // Abbildung Eingaben DTO <-> Projektgrößen
        // =================================================================================

        /// <summary>
        /// Die Eingaben der Überlagerung aus dem Stand des Kerns: Projektgrößen (nullbar =
        /// Vorgabe) und ein noch ungespeicherter Entwurf; Erzeugerart und Werkstoff aus der
        /// gespeicherten Wahl (Schritt 124), sonst „keine Angabe".
        /// </summary>
        internal static ZapfprofilAuslegungEingabeDaten AuslegungAusStand(ZapfprofilStand stand)
        {
            ProjektStand p = stand?.Projekt;
            var a = new ZapfprofilAuslegungEingabeDaten();
            if (p != null)
            {
                a.Quelle = p.BedarfstagQuelle.HasValue ? (ZapfprofilBedarfstagquelle)(int)p.BedarfstagQuelle.Value
                                                       : ZapfprofilBedarfstagquelle.Vorgaberegel;
                a.IdBedarfstag = p.IdBedarfstag;
                a.SpeicherC = p.SpeicherC;
                a.ErzeugerKw = p.ErzeugerKw;
                a.UebertragerKw = p.UebertragerKw;
                a.Speicherart = Enum.IsDefined(typeof(ZapfprofilSpeicherart), (int)p.Speicherart)
                    ? (ZapfprofilSpeicherart)(int)p.Speicherart : ZapfprofilSpeicherart.Ladespeicher;
                a.SensorhoeheAnteil = p.SensorhoeheAnteil;
                a.PunktVolumenL = p.AuslegungVolumenL;
                a.PunktLeistungKw = p.AuslegungLeistungKw;
                // Die Stochastik der Auslegung (Z3): Perzentil (nur aus der Wertemenge) und die
                // Realisierungen des Bedarfstags; „Stochastisch rechnen" trägt keine Spalte.
                a.Perzentil = TwwSchema.Perzentile.Contains(p.Perzentil) ? p.Perzentil : (int?)null;
                a.RealisierungenAuslegung = p.RealisierungenAuslegung;
                // Der Verfahrensvergleich (4.7; Stufe Z4): Ladeleistung, Ladefenster, Nutzanteil und
                // Zuschlag, Personen und Bezug des Füllstands aus den Projektgrößen (Schritt 124).
                a.LadeAuto = p.LadeAuto;
                a.LadeManuellKw = p.LadeManuellKw;
                a.LadefensterH = p.LadefensterH;
                a.LadefensterBeginnH = p.LadefensterBeginnH;
                a.Nutzanteil = p.Nutzanteil;
                a.Zuschlag = p.Zuschlag;
                a.Erzeugerart = AlsErzeugerart(p.Erzeugerart);
                a.Werkstoff = p.UebertragerWerkstoff == ZapfUebertragerwerkstoff.Stahl ? ZapfprofilWerkstoff.Stahl
                            : p.UebertragerWerkstoff == ZapfUebertragerwerkstoff.Edelstahl ? ZapfprofilWerkstoff.Edelstahl
                            : ZapfprofilWerkstoff.KeineAngabe;
                a.PersonenAuto = p.PersonenAuto;
                a.PersonenManuell = p.PersonenManuell;
                a.FuellstandBezug = p.FuellstandBezug.HasValue ? (ZapfprofilFuellstandbezug)(int)p.FuellstandBezug.Value
                                                               : ZapfprofilFuellstandbezug.Vorgabe;
            }
            if (stand?.BedarfstagEntwurf != null)
            {
                a.Quelle = ZapfprofilBedarfstagquelle.Konstruktor;
                a.IdBedarfstag = null;
                a.Entwurf = AlsBedarfstag(stand.BedarfstagEntwurf, true);
            }
            // Die Zeilen des Konstruktors (Schemaschritt T5, ZU25): Sie kommen aus dem Arbeitsstand
            // ODER aus der Datenbank — ein gespeicherter Konstruktortag hat keinen Entwurf mehr,
            // seine Zeilen stehen an Tab_TwwKonstruktorzeile. Beide Wege enden hier.
            a.Konstruktorzeilen = (stand?.Konstruktorzeilen ?? new KonstruktorzeileStand[0])
                .Where(z => z != null).Select(AlsKonstruktorzeile).ToList();
            if (a.Entwurf != null)
                a.Entwurf.Konstruktorzeilen = a.Konstruktorzeilen.Select(z => z.Kopie()).ToList();
            KonstruktorBezugSetzen(a, stand?.KonstruktorBezug);
            return a;
        }

        /// <summary>
        /// Der Bezug eines <b>gespeicherten</b> Konstruktortags an die Eingaben (Folge (a) aus N21):
        /// Bezugsart und Bezugsmenge seiner Katalogzeile, mit denen der wieder geöffnete Konstruktor
        /// beginnt — ein erneutes OK baut so denselben Tag. Nur bei der Quelle Konstruktor ohne
        /// Entwurf (der Entwurf trägt seinen Bezug selbst). Trägt die Katalogzeile keinen
        /// vollständigen Bezug oder steht sie nicht mehr, beginnt der Konstruktor ohne Bezug — mit
        /// benanntem Hinweis, nie still.
        /// </summary>
        internal static void KonstruktorBezugSetzen(ZapfprofilAuslegungEingabeDaten a, KonstruktorBezugStand bezug)
        {
            if (a == null) return;
            a.KonstruktorBezugsart = null;
            a.KonstruktorBezugsmenge = null;
            a.KonstruktorBezugHinweis = "";
            if (a.Quelle != ZapfprofilBedarfstagquelle.Konstruktor || a.Entwurf != null || bezug == null) return;
            if (!bezug.TagGefunden)
                a.KonstruktorBezugHinweis = Text_("ZPG_AUS_KON_BEZUG_TAG_FEHLT",
                    "Der gespeicherte Bedarfstag steht nicht mehr im Katalog — sein Bezug ist nicht bekannt; der Konstruktor beginnt ohne Bezug.");
            else if (bezug.Vollstaendig)
            {
                a.KonstruktorBezugsart = (int)bezug.Bezugsart.Value;
                a.KonstruktorBezugsmenge = bezug.Bezugsmenge;
            }
            else
                a.KonstruktorBezugHinweis = Text_("ZPG_AUS_KON_BEZUG_OHNE",
                    "Der gespeicherte Bedarfstag trägt keinen vollständigen Bezug (Bezugsart und Bezugsmenge) — der Konstruktor beginnt ohne Bezug, und ein OK baut den Tag unskaliert.");
        }

        /// <summary>Eine Zeile des Konstruktors aus dem Stand des Kerns — dieselben Felder, dieselbe Reihenfolge.</summary>
        private static ZapfprofilKonstruktorZeileDaten AlsKonstruktorzeile(KonstruktorzeileStand z)
            => new ZapfprofilKonstruktorZeileDaten
            {
                BeginnH = z.BeginnH, EndeH = z.EndeH, Regel = z.Regel ?? "", Anzahl = z.Anzahl, VolumenL = z.VolumenL,
                ZapftemperaturC = z.ZapftemperaturC, Verbraucher = z.Verbraucher ?? ""
            };

        /// <summary>
        /// Die Zeilen des Konstruktors aus den Eingaben der Überlagerung — nur bei der Quelle
        /// Konstruktor, sonst keine (eine andere Quelle wirft den konstruierten Tag weg, also auch
        /// seine Zeilen).
        ///
        /// <para>Mit Entwurf gelten dessen Zeilen — der Konstruktor hat sie gerade gebaut —, sonst
        /// die des geladenen Stands: Wer die Auslegung speichert, ohne den Konstruktor zu öffnen,
        /// behält die Zeilen, die schon in der Datenbank stehen (Schemaschritt T5, ZU25).</para>
        /// </summary>
        internal static IReadOnlyList<KonstruktorzeileStand> Konstruktorzeilen(ZapfprofilAuslegungEingabeDaten a)
        {
            if (a == null || a.Quelle != ZapfprofilBedarfstagquelle.Konstruktor) return new KonstruktorzeileStand[0];
            IEnumerable<ZapfprofilKonstruktorZeileDaten> zeilen =
                a.Entwurf != null && a.Entwurf.Konstruktorzeilen.Count > 0
                    ? a.Entwurf.Konstruktorzeilen
                    : (IEnumerable<ZapfprofilKonstruktorZeileDaten>)a.Konstruktorzeilen;
            return (zeilen ?? new ZapfprofilKonstruktorZeileDaten[0])
                .Where(z => z != null)
                .Select(z => new KonstruktorzeileStand(z.BeginnH, z.EndeH, z.Regel, z.Anzahl, z.VolumenL, z.ZapftemperaturC, z.Verbraucher))
                .ToList().AsReadOnly();
        }

        /// <summary>
        /// Die Projektgrößen mit den Eingaben der Überlagerung: Bedarfstag (Quelle und Katalogtag;
        /// beim Entwurf ohne Id), Speichertemperatur, Erzeuger- und Übertragerleistung, Speicherart,
        /// Sensorhöhe, der übernommene Punkt und — Stufe Z3 — Perzentil (<c>null</c> = der Wert
        /// der Basis bleibt) und Realisierungen des Bedarfstags (<c>null</c> = Vorgabe). Sonst
        /// bleibt <c>null</c> <c>null</c>. Der Punkt gilt für
        /// Übernahme und Speichern; der Rechenweg nimmt ihn mit <see cref="OhnePunkt"/> wieder heraus.
        /// </summary>
        internal static ProjektStand MitAuslegung(ProjektStand p, ZapfprofilAuslegungEingabeDaten a)
        {
            if (p == null || a == null) return p;
            bool entwurf = a.Quelle == ZapfprofilBedarfstagquelle.Konstruktor && a.Entwurf != null;
            return p with
            {
                BedarfstagQuelle = a.Quelle == ZapfprofilBedarfstagquelle.Vorgaberegel
                    ? (ZapfBedarfstagquelle?)null : (ZapfBedarfstagquelle)(int)a.Quelle,
                IdBedarfstag = entwurf || a.Quelle == ZapfprofilBedarfstagquelle.Vorgaberegel
                               || a.Quelle == ZapfprofilBedarfstagquelle.Stundenprofil
                               || a.Quelle == ZapfprofilBedarfstagquelle.Din4708Profil
                    ? null : a.IdBedarfstag,
                SpeicherC = a.SpeicherC,
                ErzeugerKw = a.ErzeugerKw,
                UebertragerKw = a.UebertragerKw,
                Speicherart = (ZapfSpeicherart)(int)a.Speicherart,
                SensorhoeheAnteil = a.SensorhoeheAnteil,
                AuslegungVolumenL = a.PunktVolumenL,
                AuslegungLeistungKw = a.PunktLeistungKw,
                Perzentil = a.Perzentil ?? p.Perzentil,
                RealisierungenAuslegung = a.RealisierungenAuslegung,
                LadeAuto = a.LadeAuto,
                LadeManuellKw = a.LadeManuellKw,
                LadefensterH = a.LadefensterH,
                LadefensterBeginnH = a.LadefensterBeginnH,
                Nutzanteil = a.Nutzanteil,
                Zuschlag = a.Zuschlag,
                Erzeugerart = AlsErzeugerart(a.Erzeugerart),
                UebertragerWerkstoff = AlsWerkstoff(a.Werkstoff),
                PersonenAuto = a.PersonenAuto,
                PersonenManuell = a.PersonenManuell,
                FuellstandBezug = a.FuellstandBezug == ZapfprofilFuellstandbezug.Vorgabe
                    ? (ZapfFuellstandbezug?)null : (ZapfFuellstandbezug)(int)a.FuellstandBezug
            };
        }

        /// <summary>
        /// Die Projektgrößen ohne Auslegungspunkt — für den Rechenweg der Überlagerung und für einen
        /// überholten Punkt (<see cref="ZapfprofilEingabeDaten.PunktUeberholt"/>). <c>null</c> bleibt <c>null</c>.
        /// </summary>
        internal static ProjektStand OhnePunkt(ProjektStand p)
            => p == null ? null : p with { AuslegungVolumenL = null, AuslegungLeistungKw = null };

        /// <summary>Der Entwurf der Eingaben als Katalogzeile des Kerns (Quelle Konstruktor); <c>null</c> ohne Entwurf.</summary>
        internal static BedarfstagKatalogzeile EntwurfAus(ZapfprofilAuslegungEingabeDaten a)
        {
            if (a?.Entwurf == null || a.Quelle != ZapfprofilBedarfstagquelle.Konstruktor) return null;
            ZapfprofilBedarfstagDaten t = a.Entwurf;
            // Bezugsmenge und Bezugsart gehen mit dem Tag (Schritt 124, N10 (j)) — nur zusammen.
            bool mitBezug = t.Bezugsmenge.HasValue && t.Bezugsart.HasValue;
            return new BedarfstagKatalogzeile(ZapfprofilCtrl.ENTWURF_ID, (t.Bezeichner ?? "").Trim(), t.Katalogversion ?? "",
                ZapfBedarfstagquelle.Konstruktor, mitBezug ? t.Bezugsmenge : null,
                new Provenienz(TwwNutzungsartCtrl.QUELLE_EIGENKONSTRUKTION, null, t.Katalogversion ?? "", Herkunftsart.Eigenkonstruktion),
                t.Ereignisse.Select(e => new Zapfereignis(e.MinuteBeginn, e.DauerMin, e.EnergieKwh)).ToArray())
            {
                Status = ZapfKatalogstatus.Eigen,
                Bezugsart = mitBezug ? (ZapfBezugsart)t.Bezugsart.Value : (ZapfBezugsart?)null
            };
        }

        /// <summary>
        /// Ein Bedarfstag des Kerns als DTO: Herkunft als Kurztext, Tagessumme und Minutenspitze;
        /// ein Normtag des Katalogs ist nicht als Katalogtag wählbar (er rechnet als DIN-4708-Profil,
        /// N10 (b)); ein ungültiger Tag ist gesperrt mit Grund. Ereignisse nur beim Entwurf.
        /// </summary>
        internal static ZapfprofilBedarfstagDaten AlsBedarfstag(BedarfstagKatalogzeile t, bool entwurf)
        {
            var d = new ZapfprofilBedarfstagDaten
            {
                Id = entwurf ? 0 : t.Id,
                Bezeichner = t.Bezeichner ?? "",
                Quelle = (ZapfprofilBedarfstagquelle)(int)t.QuelleArt,
                Herkunft = Herkunft(t.Herkunft),
                Katalogversion = t.Katalogversion ?? "",
                Bezugsmenge = t.Bezugsmenge,
                Bezugsart = t.Bezugsart.HasValue ? (int)t.Bezugsart.Value : (int?)null
            };
            if (entwurf)
                d.Ereignisse = t.Ereignisse.Select(e => new ZapfprofilEreignisDaten(e.MinuteBeginn, e.DauerMin, e.EnergieKwh)).ToList();
            try
            {
                Bedarfstag tag = Bedarfstag.AusKatalog(t, 1.0);
                d.TagessummeKwh = tag.TagessummeKwh;
                d.MinutenspitzeKw = tag.GroessteMinutenleistungKw;
            }
            catch (ZapfAuslegungException ex)
            {
                d.Waehlbar = false;
                d.Sperrgrund = Satztext(ex.Satz);
            }
            if (!entwurf && t.QuelleArt == ZapfBedarfstagquelle.Din4708Profil)
            {
                d.Waehlbar = false;
                d.Sperrgrund = Text_("ZPG_AUS_GRUND_NORMTAG",
                    "Ein Normtag des Katalogs rechnet als DIN-4708-Profil aus der Wohnungstabelle.");
            }
            return d;
        }

        private static ZapfErzeugerart? AlsErzeugerart(ZapfprofilErzeugerart a)
            => a == ZapfprofilErzeugerart.Kessel ? ZapfErzeugerart.Kessel
             : a == ZapfprofilErzeugerart.Waermepumpe ? ZapfErzeugerart.Waermepumpe : (ZapfErzeugerart?)null;

        private static ZapfprofilErzeugerart AlsErzeugerart(ZapfErzeugerart? a)
            => a == ZapfErzeugerart.Kessel ? ZapfprofilErzeugerart.Kessel
             : a == ZapfErzeugerart.Waermepumpe ? ZapfprofilErzeugerart.Waermepumpe : ZapfprofilErzeugerart.KeineAngabe;

        private static ZapfUebertragerwerkstoff? AlsWerkstoff(ZapfprofilWerkstoff w)
            => w == ZapfprofilWerkstoff.Stahl ? ZapfUebertragerwerkstoff.Stahl
             : w == ZapfprofilWerkstoff.Edelstahl ? ZapfUebertragerwerkstoff.Edelstahl : (ZapfUebertragerwerkstoff?)null;

        // =================================================================================
        // Konstruktor
        // =================================================================================

        /// <summary>
        /// <b>Der Konstruktor</b> (A100, NA.5.2.3): prüft die Zeilen der Oberfläche benannt
        /// (Name, Fenster, Menge, Regel), baut daraus die Zeilen des Kerns und über
        /// <see cref="ZapfprofilCtrl.BedarfstagKonstruieren"/> den Entwurf bei θ_KW,A des
        /// Parametersatzes. Geschrieben wird nichts — der Entwurf geht mit dem Arbeitsstand,
        /// samt den Zeilen, aus denen er entstand (ein erneutes Öffnen beginnt mit ihnen). Den
        /// Namen prüft er schon hier LESEND gegen die Katalogversion
        /// (<see cref="ZapfprofilCtrl.FreierBedarfstagname"/>) und nennt einen freien; der
        /// Schreibweg prüft ihn erneut.
        ///
        /// <para><b>Bezugsart und Bezugsmenge</b> (Schritt 124, N10 (j); Stufe Z4, Gruppe 2b) gehen
        /// nur zusammen mit dem Tag: beide leer = ein Tag des Projekts, der nie skaliert wird; eine
        /// Menge größer 0 samt Bezugsart aus der Wertemenge des Schemas = ein Tag, den die Auslegung
        /// auf die Bezugsmenge einer Gruppe derselben Bezugsart skaliert. Eine halbe Angabe lehnt der
        /// Konstruktor benannt ab (<c>ZPG_AUS_KON_BEZUG_UNVOLLSTAENDIG</c>).</para>
        /// </summary>
        internal static ZapfprofilKonstruktorErgebnis BedarfstagKonstruieren(IReadOnlyList<ZapfprofilKonstruktorZeileDaten> zeilen,
                                                                            string bezeichner, double? bezugsmenge = null,
                                                                            int? bezugsart = null)
        {
            var meldungen = new List<ZapfprofilMeldung>();
            string name = (bezeichner ?? "").Trim();
            if (name.Length == 0)
                meldungen.Add(Fehler("ZPG_AUS_KON_OHNE_NAME", "", Text_("ZPG_AUS_KON_OHNE_NAME", "Bitte einen Namen für den Bedarfstag eingeben.")));
            if (zeilen == null || zeilen.Count == 0)
                meldungen.Add(Fehler("ZPG_AUS_KON_OHNE_ZEILE", "", Text_("ZPG_AUS_KON_OHNE_ZEILE", "Mindestens eine Zeile eintragen.")));
            bool bezugGueltig = bezugsmenge.HasValue == bezugsart.HasValue
                                && (!bezugsmenge.HasValue || (bezugsmenge.Value > 0 && !double.IsInfinity(bezugsmenge.Value)))
                                && (!bezugsart.HasValue || TwwSchema.Werte(TwwSchema.BEZUGSART_WERTE).Contains(bezugsart.Value));
            if (!bezugGueltig)
                meldungen.Add(Fehler("ZPG_AUS_KON_BEZUG_UNVOLLSTAENDIG", "", Text_("ZPG_AUS_KON_BEZUG_UNVOLLSTAENDIG",
                    "Bezugsart und Bezugsmenge (größer 0) gehören zusammen.")));

            Parametersatz ps;
            try { ps = ZapfprofilCtrl.Parameter(); }
            catch (ParametersatzException ex)
            {
                meldungen.Add(new ZapfprofilMeldung(Satzkennung(ex.Satz, ""), "", Satztext(ex.Satz),
                                                    ZapfprofilMeldungsart.Fehler, ex.Message));
                return new ZapfprofilKonstruktorErgebnis(null, meldungen);
            }
            if (name.Length > 0)
            {
                string frei = ZapfprofilCtrl.FreierBedarfstagname(name, ps.Katalogversion);
                if (!string.Equals(frei, name, StringComparison.Ordinal))
                    meldungen.Add(Fehler("ZPG_AUS_KON_NAME_BELEGT", "", Format(Text_("ZPG_AUS_KON_NAME_BELEGT",
                        "Der Name „{0}“ ist in der Katalogversion „{1}“ schon vergeben — frei ist etwa „{2}“."),
                        name, ps.Katalogversion, frei)));
            }
            IReadOnlyList<Zapfregel> regeln;
            try { regeln = ZapfprofilCtrl.Konstruktorregeln(ps); }
            catch (Exception ex) when (ex is ZapfAuslegungException || ex is ParametersatzException) { regeln = new Zapfregel[0]; }

            var kern = new List<Konstruktorzeile>();
            for (int i = 0; zeilen != null && i < zeilen.Count; i++)
            {
                ZapfprofilKonstruktorZeileDaten z = zeilen[i];
                string nummer = (i + 1).ToString(CultureInfo.CurrentCulture);
                string grund = null;
                int beginn = 0, ende = 0;
                if (z == null || !z.BeginnH.HasValue || !z.EndeH.HasValue || double.IsNaN(z.BeginnH.Value) || double.IsNaN(z.EndeH.Value))
                    grund = Text_("ZPG_AUS_KON_FEHLT_FENSTER", "Beginn und Ende fehlen oder liegen nicht im Tag (0 ≤ Beginn < Ende ≤ 24 h).");
                else
                {
                    beginn = (int)Math.Round(z.BeginnH.Value * Bedarfstag.MINUTEN_JE_STUNDE, MidpointRounding.AwayFromZero);
                    ende = (int)Math.Round(z.EndeH.Value * Bedarfstag.MINUTEN_JE_STUNDE, MidpointRounding.AwayFromZero);
                    if (beginn < 0 || beginn >= Bedarfstag.MINUTEN || ende <= beginn || ende > Bedarfstag.MINUTEN)
                        grund = Text_("ZPG_AUS_KON_FEHLT_FENSTER", "Beginn und Ende fehlen oder liegen nicht im Tag (0 ≤ Beginn < Ende ≤ 24 h).");
                }

                if (grund == null)
                {
                    string verbraucher = (z.Verbraucher ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(z.Regel))
                    {
                        Zapfregel regel = regeln.FirstOrDefault(r => string.Equals(r.Name, z.Regel, StringComparison.Ordinal));
                        if (regel == null)
                            grund = Format(Text_("ZPG_AUS_KON_REGEL_FEHLT", "Die Zapfregel „{0}“ steht nicht im Katalog."), z.Regel);
                        else if (!(z.Anzahl >= 0))
                            grund = Text_("ZPG_AUS_KON_FEHLT_MENGE", "Anzahl bzw. Volumen und Zapftemperatur fehlen.");
                        else
                            kern.Add(Konstruktorzeile.AusVorgaengen(beginn, ende, z.Anzahl.Value, regel,
                                                                     verbraucher.Length > 0 ? verbraucher : null));
                    }
                    else if (!(z.VolumenL >= 0) || !z.ZapftemperaturC.HasValue || double.IsNaN(z.ZapftemperaturC.Value))
                        grund = Text_("ZPG_AUS_KON_FEHLT_MENGE", "Anzahl bzw. Volumen und Zapftemperatur fehlen.");
                    else
                        kern.Add(new Konstruktorzeile(beginn, ende, z.VolumenL.Value, z.ZapftemperaturC.Value, verbraucher));
                }

                if (grund != null)
                    meldungen.Add(Fehler("ZPG_AUS_KON_ZEILE", "", Format(Text_("ZPG_AUS_KON_ZEILE", "Zeile {0}: {1}"), nummer, grund)));
            }
            if (meldungen.Count > 0) return new ZapfprofilKonstruktorErgebnis(null, meldungen);

            try
            {
                BedarfstagKatalogzeile t = ZapfprofilCtrl.BedarfstagKonstruieren(kern, name, ps, bezugsmenge,
                    bezugsart.HasValue ? (ZapfBezugsart)bezugsart.Value : (ZapfBezugsart?)null);
                ZapfprofilBedarfstagDaten tag = AlsBedarfstag(t, true);
                tag.Konstruktorzeilen = zeilen.Select(z => z.Kopie()).ToList();
                return new ZapfprofilKonstruktorErgebnis(tag, new ZapfprofilMeldung[0]);
            }
            catch (Exception ex) when (ex is ZapfAuslegungException || ex is ParametersatzException)
            {
                ZapfSatz satz = SatzAus(ex);
                meldungen.Add(new ZapfprofilMeldung(Satzkennung(satz, "ZPG_AUS_KON_NICHT_GEBAUT"), "",
                    Format(Text_("ZPG_AUS_KON_NICHT_GEBAUT", "Der Bedarfstag wurde nicht gebaut — {0}"), Satztext(satz)),
                    ZapfprofilMeldungsart.Fehler, ex.Message));
                return new ZapfprofilKonstruktorErgebnis(null, meldungen);
            }
        }

        // =================================================================================
        // Schreibweg (im gemeinsamen Vorgang des Bedarfsprofil-Dialogs, 5.2)
        // =================================================================================

        /// <summary>
        /// <b>Der Schreibweg der Auslegung</b> im Vorgang des Aufrufers (kein Commit): Zonen,
        /// Projektgrößen samt Punkt und Eingaben der Überlagerung und — falls konstruiert — der
        /// Bedarfstag als Katalogzeile (Status EIGEN, Herkunftsart EIGENKONSTRUKTION, bei θ_KW,A
        /// des Parametersatzes), alles über <c>ZapfprofilCtrl.Speichern</c>. Eine benannte
        /// Ablehnung kommt als Meldung zurück; der Aufrufer rollt zurück, der Stand bleibt.
        /// </summary>
        internal static ZapfprofilSpeicherergebnis AuslegungSpeichern(int idProjekt, ZapfprofilStand stand, DbVorgang v)
            => Speichern(idProjekt, stand, v);

        // =================================================================================
        // Texte
        // =================================================================================

        /// <summary>Das Textbündel der Überlagerung in der Oberflächensprache; fehlt ein Schlüssel, bleibt der deutsche Rückfall.</summary>
        internal static ZapfprofilAuslegungTexte AuslegungTexte()
        {
            var t = new ZapfprofilAuslegungTexte();
            t.Titel = Text_("ZPG_AUS_TITEL", t.Titel);
            t.InfoBedienung = Text_("ZPG_AUS_INFO_BEDIENUNG", t.InfoBedienung);
            t.InfoRechenweg = Text_("ZPG_AUS_INFO_RECHENWEG", t.InfoRechenweg);
            t.GrundNochNicht = Text_("ZPG_AUS_GRUND_NOCH_NICHT", t.GrundNochNicht);
            t.GruppeEingaben = Text_("ZPG_AUS_GRP_EINGABEN", t.GruppeEingaben);
            t.LabelBedarfstag = Text_("ZPG_AUS_LBL_BEDARFSTAG", t.LabelBedarfstag);
            t.OptionVorgaberegel = Text_("ZPG_AUS_OPT_VORGABEREGEL", t.OptionVorgaberegel);
            t.OptionStundenprofil = Text_("ZPG_AUS_OPT_STUNDENPROFIL", t.OptionStundenprofil);
            t.OptionDin4708 = Text_("ZPG_AUS_OPT_DIN4708", t.OptionDin4708);
            t.OptionA100 = Text_("ZPG_AUS_OPT_A100", t.OptionA100);
            t.GrundA100 = Text_("ZPG_AUS_GRUND_A100", t.GrundA100);
            t.OptionEcodesign = Text_("ZPG_AUS_OPT_ECODESIGN", t.OptionEcodesign);
            t.GrundEcodesign = Text_("ZPG_AUS_GRUND_ECODESIGN", t.GrundEcodesign);
            t.OptionKatalogtag = Text_("ZPG_AUS_OPT_KATALOGTAG", t.OptionKatalogtag);
            t.OptionEntwurf = Text_("ZPG_AUS_OPT_ENTWURF", t.OptionEntwurf);
            t.KnopfKonstruieren = Text_("ZPG_AUS_BTN_KONSTRUIEREN", t.KnopfKonstruieren);
            t.HinweisKonstruktor = Text_("ZPG_AUS_HINW_KONSTRUKTOR", t.HinweisKonstruktor);
            t.BannerSpitzen = Text_("ZPG_AUS_BANNER_SPITZEN", t.BannerSpitzen);
            t.LabelSpeichertemperatur = Text_("ZPG_AUS_LBL_SPEICHERTEMPERATUR", t.LabelSpeichertemperatur);
            t.LabelErzeugerleistung = Text_("ZPG_AUS_LBL_ERZEUGERLEISTUNG", t.LabelErzeugerleistung);
            t.LabelUebertragerleistung = Text_("ZPG_AUS_LBL_UEBERTRAGERLEISTUNG", t.LabelUebertragerleistung);
            t.LabelSpeicherart = Text_("ZPG_AUS_LBL_SPEICHERART", t.LabelSpeicherart);
            t.SpeicherartLadespeicher = Text_("ZPG_AUS_SPEICHERART_LADESPEICHER", t.SpeicherartLadespeicher);
            t.SpeicherartGemischt = Text_("ZPG_AUS_SPEICHERART_GEMISCHT", t.SpeicherartGemischt);
            t.LabelSensorhoehe = Text_("ZPG_AUS_LBL_SENSORHOEHE", t.LabelSensorhoehe);
            t.LabelErzeugerart = Text_("ZPG_AUS_LBL_ERZEUGERART", t.LabelErzeugerart);
            t.LabelWerkstoff = Text_("ZPG_AUS_LBL_WERKSTOFF", t.LabelWerkstoff);
            t.KeineAngabe = Text_("ZPG_AUS_KEINE_ANGABE", t.KeineAngabe);
            t.ErzeugerKessel = Text_("ZPG_AUS_ERZEUGER_KESSEL", t.ErzeugerKessel);
            t.ErzeugerWaermepumpe = Text_("ZPG_AUS_ERZEUGER_WAERMEPUMPE", t.ErzeugerWaermepumpe);
            t.WerkstoffStahl = Text_("ZPG_AUS_WERKSTOFF_STAHL", t.WerkstoffStahl);
            t.WerkstoffEdelstahl = Text_("ZPG_AUS_WERKSTOFF_EDELSTAHL", t.WerkstoffEdelstahl);
            t.HerleitungSpeichertemperatur = Text_("ZPG_AUS_HERL_SPEICHERTEMPERATUR", t.HerleitungSpeichertemperatur);
            t.HerleitungErzeuger = Text_("ZPG_AUS_HERL_ERZEUGER", t.HerleitungErzeuger);
            t.HerleitungUebertrager = Text_("ZPG_AUS_HERL_UEBERTRAGER", t.HerleitungUebertrager);
            t.HerleitungSensorhoehe = Text_("ZPG_AUS_HERL_SENSORHOEHE", t.HerleitungSensorhoehe);
            t.LabelStochastisch = Text_("ZPG_AUS_LBL_STOCHASTISCH", t.LabelStochastisch);
            t.HerleitungStochastisch = Text_("ZPG_AUS_HERL_STOCHASTISCH", t.HerleitungStochastisch);
            t.LabelPerzentil = Text_("ZPG_AUS_LBL_PERZENTIL", t.LabelPerzentil);
            t.HerleitungPerzentil = Text_("ZPG_AUS_HERL_PERZENTIL", t.HerleitungPerzentil);
            t.LabelRealisierungen = Text_("ZPG_AUS_LBL_REALISIERUNGEN", t.LabelRealisierungen);
            t.EinheitTage = Text_("ZPG_AUS_EINHEIT_TAGE", t.EinheitTage);
            t.HerleitungRealisierungen = Text_("ZPG_AUS_HERL_REALISIERUNGEN", t.HerleitungRealisierungen);
            t.HerleitungRealisierungenOhne = Text_("ZPG_AUS_HERL_REALISIERUNGEN_OHNE", t.HerleitungRealisierungenOhne);
            t.MeldungFehleingabe = Text_("ZPG_AUS_MSG_FEHLEINGABE", t.MeldungFehleingabe);
            t.GruppeTopologie = Text_("ZPG_AUS_GRP_TOPOLOGIE", t.GruppeTopologie);
            t.BedarfstagGruppe = Text_("ZPG_AUS_BEDARFSTAG_GRUPPE", t.BedarfstagGruppe);
            t.KarteSummenlinie = Text_("ZPG_AUS_KARTE_SUMMENLINIE", t.KarteSummenlinie);
            t.KarteSummenlinieUnter = Text_("ZPG_AUS_KARTE_SUMMENLINIE_UNTER", t.KarteSummenlinieUnter);
            t.KarteMinutenspitze = Text_("ZPG_AUS_KARTE_MINUTENSPITZE", t.KarteMinutenspitze);
            t.KarteMinutenspitzeUnter = Text_("ZPG_AUS_KARTE_MINUTENSPITZE_UNTER", t.KarteMinutenspitzeUnter);
            t.KartePerzentil = Text_("ZPG_AUS_KARTE_PERZENTIL", t.KartePerzentil);
            t.KartePerzentilUnter = Text_("ZPG_AUS_KARTE_PERZENTIL_UNTER", t.KartePerzentilUnter);
            t.PerzentilOffen = Text_("ZPG_AUS_PERZENTIL_OFFEN", t.PerzentilOffen);
            t.PerzentilLauf = Text_("ZPG_AUS_PERZENTIL_LAUF", t.PerzentilLauf);
            t.Entfaellt = Text_("ZPG_AUS_ENTFAELLT", t.Entfaellt);
            t.PerzentilEntfaellt = Text_("ZPG_AUS_PERZENTIL_ENTFAELLT", t.PerzentilEntfaellt);
            t.PerzentilLaeuft = Text_("ZPG_AUS_PERZENTIL_LAEUFT", t.PerzentilLaeuft);
            t.StatusEnsembleLaeuft = Text_("ZPG_AUS_STATUS_LAEUFT", t.StatusEnsembleLaeuft);
            t.HinweisEnsembleAbgebrochen = Text_("ZPG_AUS_HINW_ENSEMBLE_ABGEBROCHEN", t.HinweisEnsembleAbgebrochen);
            t.PerzentilVolumen = Text_("ZPG_AUS_PERZENTIL_VOLUMEN", t.PerzentilVolumen);
            t.PerzentilVolumenWert = Text_("ZPG_AUS_PERZENTIL_VOLUMEN_WERT", t.PerzentilVolumenWert);
            t.PerzentilMinutenspitze = Text_("ZPG_AUS_PERZENTIL_MINUTENSPITZE", t.PerzentilMinutenspitze);
            t.PerzentilNachrichtlich = Text_("ZPG_AUS_PERZENTIL_NACHRICHTLICH", t.PerzentilNachrichtlich);
            t.NichtBelastbar = Text_("ZPG_AUS_NICHT_BELASTBAR", t.NichtBelastbar);
            t.GrundNichtBelastbar = Text_("ZPG_AUS_GRUND_NICHT_BELASTBAR", t.GrundNichtBelastbar);
            t.Streuband = Text_("ZPG_AUS_STREUBAND", t.Streuband);
            t.SpaltePerzentil = Text_("ZPG_AUS_SP_PERZENTIL", t.SpaltePerzentil);
            t.SpalteWert = Text_("ZPG_AUS_SP_WERT", t.SpalteWert);
            t.Spannweite = Text_("ZPG_AUS_SPANNWEITE", t.Spannweite);
            t.OhneNachweis = Text_("ZPG_AUS_OHNE_NACHWEIS", t.OhneNachweis);
            t.GlfV = Text_("ZPG_AUS_GLF_V", t.GlfV);
            t.GlfVBezug = Text_("ZPG_AUS_GLF_V_BEZUG", t.GlfVBezug);
            t.GlfP = Text_("ZPG_AUS_GLF_P", t.GlfP);
            t.GlfPBezug = Text_("ZPG_AUS_GLF_P_BEZUG", t.GlfPBezug);
            t.WurzelN = Text_("ZPG_AUS_WURZEL_N", t.WurzelN);
            t.SpitzeJeEinheit = Text_("ZPG_AUS_SPITZE_JE_EINHEIT", t.SpitzeJeEinheit);
            t.SpitzeJeEinheitZone = Text_("ZPG_AUS_SPITZE_JE_EINHEIT_ZONE", t.SpitzeJeEinheitZone);
            t.KarteNorm = Text_("ZPG_AUS_KARTE_NORM", t.KarteNorm);
            t.KarteNormUnter = Text_("ZPG_AUS_KARTE_NORM_UNTER", t.KarteNormUnter);
            t.GewaehlterPunkt = Text_("ZPG_AUS_GEWAEHLTER_PUNKT", t.GewaehlterPunkt);
            t.Ladezeit = Text_("ZPG_AUS_LADEZEIT", t.Ladezeit);
            t.Zeitkonstante = Text_("ZPG_AUS_ZEITKONSTANTE", t.Zeitkonstante);
            t.WertepaareOhne = Text_("ZPG_AUS_WERTEPAARE_OHNE", t.WertepaareOhne);
            t.Bedarfskennzahl = Text_("ZPG_AUS_BEDARFSKENNZAHL", t.Bedarfskennzahl);
            t.Vergleichspunkt = Text_("ZPG_AUS_VERGLEICHSPUNKT", t.Vergleichspunkt);
            t.ZonenAusserhalb = Text_("ZPG_AUS_ZONEN_AUSSERHALB", t.ZonenAusserhalb);
            t.HinweisWaermepumpe = Text_("ZPG_AUS_HINW_WAERMEPUMPE", t.HinweisWaermepumpe);
            t.Rohrnetz = Text_("ZPG_AUS_ROHRNETZ", t.Rohrnetz);
            t.GrundRohrnetz = Text_("ZPG_AUS_GRUND_ROHRNETZ", t.GrundRohrnetz);
            t.StandNichtRechenbar = Text_("ZPG_AUS_STAND_NICHT_RECHENBAR", t.StandNichtRechenbar);
            t.StandAusserhalb = Text_("ZPG_AUS_STAND_AUSSERHALB", t.StandAusserhalb);
            t.Empfehlung = Text_("ZPG_AUS_EMPFEHLUNG", t.Empfehlung);
            t.PunktSpeicher = Text_("ZPG_AUS_PUNKT_SPEICHER", t.PunktSpeicher);
            t.PunktNenninhalt = Text_("ZPG_AUS_PUNKT_NENNINHALT", t.PunktNenninhalt);
            t.PunktMinutenspitze = Text_("ZPG_AUS_PUNKT_MINUTENSPITZE", t.PunktMinutenspitze);
            t.EmpfehlungKeine = Text_("ZPG_AUS_EMPFEHLUNG_KEINE", t.EmpfehlungKeine);
            t.Schnellauslegung = Text_("ZPG_AUS_SCHNELLAUSLEGUNG", t.Schnellauslegung);
            t.Entwurfsstand = Text_("ZPG_AUS_ENTWURFSSTAND", t.Entwurfsstand);
            t.Nachrichtlich = Text_("ZPG_AUS_NACHRICHTLICH", t.Nachrichtlich);
            t.PunktBleibt = Text_("ZPG_AUS_PUNKT_BLEIBT", t.PunktBleibt);
            t.Vergleich = Text_("ZPG_AUS_VERGLEICH", t.Vergleich);
            t.VergleichMarke = Text_("ZPG_AUS_VERGLEICH_MARKE", t.VergleichMarke);
            t.VergleichUnter = Text_("ZPG_AUS_VERGLEICH_UNTER", t.VergleichUnter);
            t.VergleichEingaben = Text_("ZPG_AUS_VERGLEICH_EINGABEN", t.VergleichEingaben);
            t.Auto = Text_("ZPG_AUS_AUTO", t.Auto);
            t.Manuell = Text_("ZPG_AUS_MANUELL", t.Manuell);
            t.SpalteVerfahren = Text_("ZPG_AUS_SP_VERFAHREN", t.SpalteVerfahren);
            t.SpalteVolumen = Text_("ZPG_AUS_SP_VOLUMEN", t.SpalteVolumen);
            t.SpalteKennwert = Text_("ZPG_AUS_SP_KENNWERT", t.SpalteKennwert);
            t.SpalteRechenweg = Text_("ZPG_AUS_SP_RECHENWEG", t.SpalteRechenweg);
            t.GroessterWert = Text_("ZPG_AUS_GROESSTER_WERT", t.GroessterWert);
            t.NurNachrichtlich = Text_("ZPG_AUS_NUR_NACHRICHTLICH", t.NurNachrichtlich);
            t.KachelGroesster = Text_("ZPG_AUS_KACHEL_GROESSTER", t.KachelGroesster);
            t.KachelListe = Text_("ZPG_AUS_KACHEL_LISTE", t.KachelListe);
            t.KachelKriterium = Text_("ZPG_AUS_KACHEL_KRITERIUM", t.KachelKriterium);
            t.KachelFuellstand = Text_("ZPG_AUS_KACHEL_FUELLSTAND", t.KachelFuellstand);
            t.Nenninhalt = Text_("ZPG_AUS_NENNINHALT", t.Nenninhalt);
            t.Mehrspeicher = Text_("ZPG_AUS_MEHRSPEICHER", t.Mehrspeicher);
            t.OhneListe = Text_("ZPG_AUS_OHNE_LISTE", t.OhneListe);
            t.KriteriumNl = Text_("ZPG_AUS_KRITERIUM_NL", t.KriteriumNl);
            t.KriteriumOhne = Text_("ZPG_AUS_KRITERIUM_OHNE", t.KriteriumOhne);
            t.Fuellstand = Text_("ZPG_AUS_FUELLSTAND", t.Fuellstand);
            t.Zeitpunkt = Text_("ZPG_AUS_ZEITPUNKT", t.Zeitpunkt);
            t.DmaxNull = Text_("ZPG_AUS_DMAX_NULL", t.DmaxNull);
            t.Wochenbild = Text_("ZPG_AUS_WOCHENBILD", t.Wochenbild);
            t.KnopfUebergeben = Text_("ZPG_AUS_BTN_UEBERGEBEN", t.KnopfUebergeben);
            t.GrundUebergeben = Text_("ZPG_AUS_GRUND_UEBERGEBEN", t.GrundUebergeben);
            t.Warnliste = Text_("ZPG_AUS_WARNLISTE", t.Warnliste);
            t.WarnlisteUnter = Text_("ZPG_AUS_WARNLISTE_UNTER", t.WarnlisteUnter);
            t.WarnlisteLeer = Text_("ZPG_AUS_WARNLISTE_LEER", t.WarnlisteLeer);
            t.GruppeHerkunft = Text_("ZPG_AUS_GRP_HERKUNFT", t.GruppeHerkunft);
            t.HerkunftUnter = Text_("ZPG_AUS_HERKUNFT_UNTER", t.HerkunftUnter);
            t.HerkunftLeer = Text_("ZPG_AUS_HERKUNFT_LEER", t.HerkunftLeer);
            t.HerkunftSpalteGroesse = Text_("ZPG_AUS_HERKUNFT_SP_GROESSE", t.HerkunftSpalteGroesse);
            t.HerkunftSpalteWert = Text_("ZPG_AUS_HERKUNFT_SP_WERT", t.HerkunftSpalteWert);
            t.HerkunftSpalteZone = Text_("ZPG_AUS_HERKUNFT_SP_ZONE", t.HerkunftSpalteZone);
            t.HerkunftSpalteStand = Text_("ZPG_AUS_HERKUNFT_SP_STAND", t.HerkunftSpalteStand);
            t.HerkunftSpalteQuelle = Text_("ZPG_AUS_HERKUNFT_SP_QUELLE", t.HerkunftSpalteQuelle);
            t.HerkunftSpalteVermerk = Text_("ZPG_AUS_HERKUNFT_SP_VERMERK", t.HerkunftSpalteVermerk);
            t.Konsistenz = Text_("ZPG_AUS_KONSISTENZ", t.Konsistenz);
            t.GrundKonsistenz = Text_("ZPG_AUS_GRUND_KONSISTENZ", t.GrundKonsistenz);
            t.KonsistenzOk = Text_("ZPG_AUS_KONSISTENZ_OK", t.KonsistenzOk);
            t.KonsistenzAuffaellig = Text_("ZPG_AUS_KONSISTENZ_AUFFAELLIG", t.KonsistenzAuffaellig);
            t.KonsistenzOhneSchwelle = Text_("ZPG_AUS_KONSISTENZ_OHNE_SCHWELLE", t.KonsistenzOhneSchwelle);
            t.KonsistenzLaeuft = Text_("ZPG_AUS_KONSISTENZ_LAEUFT", t.KonsistenzLaeuft);
            t.KonsistenzOhnePerzentil = Text_("ZPG_AUS_KONSISTENZ_OHNE_PERZENTIL", t.KonsistenzOhnePerzentil);
            t.StufeHinweis = Text_("ZPG_AUS_STUFE_HINWEIS", t.StufeHinweis);
            t.StufeWarnung = Text_("ZPG_AUS_STUFE_WARNUNG", t.StufeWarnung);
            t.StatusPunkt = Text_("ZPG_AUS_STATUS_PUNKT", t.StatusPunkt);
            t.StatusOhnePunkt = Text_("ZPG_AUS_STATUS_OHNE_PUNKT", t.StatusOhnePunkt);
            t.StatusOhne = Text_("ZPG_AUS_STATUS_OHNE", t.StatusOhne);
            t.RueckfrageTitel = Text_("ZPG_AUS_RUECKFRAGE_TITEL", t.RueckfrageTitel);
            t.RueckfrageStundenprofil = Text_("ZPG_AUS_RUECKFRAGE_STUNDENPROFIL", t.RueckfrageStundenprofil);
            t.Ja = Text_("ZPG_AUS_JA", t.Ja);
            t.Nein = Text_("ZPG_AUS_NEIN", t.Nein);
            t.KonstruktorTitel = Text_("ZPG_AUS_KON_TITEL", t.KonstruktorTitel);
            t.KonstruktorName = Text_("ZPG_AUS_KON_LBL_NAME", t.KonstruktorName);
            t.KonstruktorHinweis = Text_("ZPG_AUS_KON_HINWEIS", t.KonstruktorHinweis);
            t.KonstruktorBeginn = Text_("ZPG_AUS_KON_SP_BEGINN", t.KonstruktorBeginn);
            t.KonstruktorEnde = Text_("ZPG_AUS_KON_SP_ENDE", t.KonstruktorEnde);
            t.KonstruktorRegel = Text_("ZPG_AUS_KON_SP_REGEL", t.KonstruktorRegel);
            t.KonstruktorAnzahl = Text_("ZPG_AUS_KON_SP_ANZAHL", t.KonstruktorAnzahl);
            t.KonstruktorVolumen = Text_("ZPG_AUS_KON_SP_VOLUMEN", t.KonstruktorVolumen);
            t.KonstruktorTemperatur = Text_("ZPG_AUS_KON_SP_TEMPERATUR", t.KonstruktorTemperatur);
            t.KonstruktorVerbraucher = Text_("ZPG_AUS_KON_SP_VERBRAUCHER", t.KonstruktorVerbraucher);
            t.KonstruktorRegelFrei = Text_("ZPG_AUS_KON_REGEL_FREI", t.KonstruktorRegelFrei);
            t.KonstruktorRegelText = Text_("ZPG_AUS_KON_REGEL", t.KonstruktorRegelText);
            t.KonstruktorZeileNeu = Text_("ZPG_AUS_KON_BTN_ZEILE_NEU", t.KonstruktorZeileNeu);
            t.KonstruktorZeileEntfernen = Text_("ZPG_AUS_KON_BTN_ZEILE_ENTFERNEN", t.KonstruktorZeileEntfernen);
            t.KonstruktorRegelnOhne = Text_("ZPG_AUS_KON_REGELN_OHNE", t.KonstruktorRegelnOhne);
            t.KonstruktorSumme = Text_("ZPG_AUS_KON_SUMME", t.KonstruktorSumme);
            t.KonstruktorFeld = Text_("ZPG_AUS_KON_FELD", t.KonstruktorFeld);
            t.KonstruktorFehleingabe = Text_("ZPG_AUS_KON_FEHLEINGABE", t.KonstruktorFehleingabe);
            t.KonstruktorGrundLetzteZeile = Text_("ZPG_AUS_KON_GRUND_LETZTE_ZEILE", t.KonstruktorGrundLetzteZeile);
            // Eingaben des Verfahrensvergleichs, Laufangaben, Konstruktor (Z4, Gruppe 2b)
            t.GruppeVergleichEingaben = Text_("ZPG_AUS_GRP_VERGLEICH_EINGABEN", t.GruppeVergleichEingaben);
            t.HinweisVergleichEingaben = Text_("ZPG_AUS_HINW_VERGLEICH_EINGABEN", t.HinweisVergleichEingaben);
            t.LabelLadeleistung = Text_("ZPG_AUS_LBL_LADELEISTUNG", t.LabelLadeleistung);
            t.LabelLadeManuell = Text_("ZPG_AUS_LBL_LADE_MANUELL", t.LabelLadeManuell);
            t.LabelLadefenster = Text_("ZPG_AUS_LBL_LADEFENSTER", t.LabelLadefenster);
            t.LabelLadefensterBeginn = Text_("ZPG_AUS_LBL_LADEFENSTER_BEGINN", t.LabelLadefensterBeginn);
            t.LabelNutzanteil = Text_("ZPG_AUS_LBL_NUTZANTEIL", t.LabelNutzanteil);
            t.LabelZuschlag = Text_("ZPG_AUS_LBL_ZUSCHLAG", t.LabelZuschlag);
            t.LabelPersonen = Text_("ZPG_AUS_LBL_PERSONEN", t.LabelPersonen);
            t.LabelPersonenManuell = Text_("ZPG_AUS_LBL_PERSONEN_MANUELL", t.LabelPersonenManuell);
            t.LabelFuellstandBezug = Text_("ZPG_AUS_LBL_FUELLSTAND_BEZUG", t.LabelFuellstandBezug);
            t.FuellstandVorgabe = Text_("ZPG_AUS_FUELLSTAND_VORGABE", t.FuellstandVorgabe);
            t.KnopfVorschlag = Text_("ZPG_AUS_BTN_VORSCHLAG", t.KnopfVorschlag);
            t.LabelVorschlag = Text_("ZPG_AUS_LBL_VORSCHLAG", t.LabelVorschlag);
            t.HerleitungLade = Text_("ZPG_AUS_HERL_LADE", t.HerleitungLade);
            t.HerleitungLadeOhne = Text_("ZPG_AUS_HERL_LADE_OHNE", t.HerleitungLadeOhne);
            t.HerleitungLadefenster = Text_("ZPG_AUS_HERL_LADEFENSTER", t.HerleitungLadefenster);
            t.HerleitungPersonen = Text_("ZPG_AUS_HERL_PERSONEN", t.HerleitungPersonen);
            t.HerleitungAngesetzt = Text_("ZPG_AUS_HERL_ANGESETZT", t.HerleitungAngesetzt);
            t.HerleitungFuellstand = Text_("ZPG_AUS_HERL_FUELLSTAND", t.HerleitungFuellstand);
            t.HerleitungGespeichert = Text_("ZPG_AUS_HERL_GESPEICHERT", t.HerleitungGespeichert);
            t.HerleitungWerkstoff = Text_("ZPG_AUS_HERL_WERKSTOFF", t.HerleitungWerkstoff);
            t.KnopfErzeugerVorschlag = Text_("ZPG_AUS_BTN_VORSCHLAG_WAEHLEN", t.KnopfErzeugerVorschlag);
            t.LabelErzeugerVorschlag = Text_("ZPG_AUS_ERZEUGERART_VORSCHLAG", t.LabelErzeugerVorschlag);
            t.KonstruktorLabelBezugsart = Text_("ZPG_AUS_KON_LBL_BEZUGSART", t.KonstruktorLabelBezugsart);
            t.KonstruktorOhneBezug = Text_("ZPG_AUS_KON_OHNE_BEZUG", t.KonstruktorOhneBezug);
            t.KonstruktorLabelBezugsmenge = Text_("ZPG_AUS_KON_LBL_BEZUGSMENGE", t.KonstruktorLabelBezugsmenge);
            t.KonstruktorHinweisBezug = Text_("ZPG_AUS_KON_HINW_BEZUG", t.KonstruktorHinweisBezug);
            return t;
        }

        /// <summary>Die Beschriftungen der Auslegungsbilder in der Oberflächensprache.</summary>
        internal static ZapfprofilAuslegungBildtexte AuslegungBildtexte()
        {
            var t = new ZapfprofilAuslegungBildtexte();
            t.TitelSummenlinie = Text_("ZPG_AUSBILD_SUMMENLINIE", t.TitelSummenlinie);
            t.Bedarf = Text_("ZPG_AUSBILD_BEDARF", t.Bedarf);
            t.Versorgung = Text_("ZPG_AUSBILD_VERSORGUNG", t.Versorgung);
            t.Speicherinhalt = Text_("ZPG_AUSBILD_SPEICHERINHALT", t.Speicherinhalt);
            t.Beruehrung = Text_("ZPG_AUSBILD_BERUEHRUNG", t.Beruehrung);
            t.AchseMinutentakt = Text_("ZPG_AUSBILD_ACHSE_MINUTENTAKT", t.AchseMinutentakt);
            t.AchseEnergie = Text_("ZPG_AUSBILD_ACHSE_ENERGIE", t.AchseEnergie);
            t.TitelWertepaare = Text_("ZPG_AUSBILD_WERTEPAARE", t.TitelWertepaare);
            t.Wertepaare = Text_("ZPG_AUSBILD_REIHE_WERTEPAARE", t.Wertepaare);
            t.Gewaehlt = Text_("ZPG_AUSBILD_GEWAEHLT", t.Gewaehlt);
            t.AchseLeistung = Text_("ZPG_ACHSE_LEISTUNG", t.AchseLeistung);
            t.AchseVolumen = Text_("ZPG_AUSBILD_ACHSE_VOLUMEN", t.AchseVolumen);
            t.TitelWoche = Text_("ZPG_AUSBILD_WOCHE", t.TitelWoche);
            t.Zapfung = Text_("ZPG_REIHE_ZAPFUNG", t.Zapfung);
            t.Zirkulation = Text_("ZPG_REIHE_ZIRKULATION", t.Zirkulation);
            t.Ladung = Text_("ZPG_AUSBILD_LADUNG", t.Ladung);
            t.Defizit = Text_("ZPG_AUSBILD_DEFIZIT", t.Defizit);
            t.Fuellstand = Text_("ZPG_AUSBILD_FUELLSTAND", t.Fuellstand);
            t.Massgebend = Text_("ZPG_AUSBILD_MASSGEBEND", t.Massgebend);
            t.AchseWochenstunde = Text_("ZPG_ACHSE_WOCHENSTUNDE", t.AchseWochenstunde);
            t.AchseSpeicher = Text_("ZPG_AUSBILD_ACHSE_SPEICHER", t.AchseSpeicher);
            return t;
        }
    }
}
