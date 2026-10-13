using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Seiten.Simulation;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die plattformfreie Naht einer Wärmepumpen-ANLAGENZEILE</b> (KM‑K2) — Lesen, Schreiben und der
    /// Parametersatz der Anlagenfelder (<c>WaermepumpeAnlageFelder</c>) OHNE Windows-Code.
    ///
    /// <para><b>Warum hier.</b> Die Anlagenzeile einer Wärmepumpe wird an drei Stellen bearbeitet: im
    /// Wärmepumpendialog (Satz-Überlagerung, AW2), an der Erzeugerkarte der Simulationskonfiguration und —
    /// seit KM‑K2 — im Kältemaschinen-Dialog, wenn dort eine Wärmepumpe gewählt ist. Lese- und Schreibweg
    /// stehen deshalb EINMAL und plattformfrei: <see cref="Lesen"/> füllt den Feldsatz (Gerätestand
    /// zweistufig nachziehen, dann <see cref="AusModell"/>), <see cref="Schreiben"/> schreibt die
    /// Konfigurationsfelder über den schmalen Kernweg <c>WErzeugerCtrl.KonfigurationSchreiben</c> — die
    /// Anlagen-Ids bleiben stehen.</para>
    ///
    /// <para><b>Was die Naht NICHT trägt</b>, sagt sie benannt statt still: Kennlinienbilder, Farbwahl,
    /// Kostenfenster, der Kennlinieneditor und „In Stamm übernehmen…" hängen an Wegen der Windows-Schale
    /// (<c>WaermepumpeAnlageHuelle</c>). Der Satz aus <see cref="Gaben"/> führt sie nicht; die Anlagenfelder
    /// lassen die zugehörigen Knöpfe dann weg. Der Wärmepumpendialog bleibt der Ort, an dem Kennlinien
    /// bearbeitet werden.</para>
    /// </summary>
    internal static class WaermepumpeAnlagenaht
    {
        // =================================================================================
        //  Lesen und Schreiben der Anlagenzeile
        // =================================================================================

        /// <summary>
        /// Die Anlagenzeile als Feldsatz — <b>derselbe</b> Weg, den die Verwaltung
        /// nimmt (<c>WaermepumpenHuelle.Gaben</c>): Gerätestand zweistufig nachziehen,
        /// dann <c>AusModell</c>. Ohne das Nachziehen stünden Nennleistung, Typ und
        /// Hersteller auf 0 bzw. leer — und <c>NachModell</c> schriebe sie so zurück
        /// (Ä22/Ä23).
        /// </summary>
        /// <returns><c>null</c> = das Projekt führt keine Anlage dieser Id.</returns>
        internal static WaermepumpeAnlageDaten Lesen(int idProjekt, int idAnlage)
        {
            WErzeugerModel m = Anlage(idProjekt, idAnlage, out _);
            if (m == null) return null;

            WaermepumpeGeraeteCtrl.GeraetedatenFuellen(m, m.ID_WP);
            WaermepumpeAnlageDaten d = AusModell(m);
            // Anwenderwunsch 08.10.2026: ohne Traeger die Vorauswahl des Kerns - sie steht im Feldsatz und
            // wird mit OK gespeichert; der Anwender kann sie aendern.
            if (d.CarrierId <= 0) d.CarrierId = ErzeugerTraegerHuelle.Vorauswahl(DbWerte.ERZEUGER_WAERMEPUMPE, 0, idProjekt);
            // Uebergabegrenze UB-E1/E2: Herleitungszeile, Schnellwahl und Kaeltemittel des Geraets; geschrieben werden
            // Vorlauf_Max (MitBetriebszeiten), Einbindung und Vorwaermbetrieb (MitUebergabe) und das Kaeltemittel.
            BivalenzAbbildung.Lesen(m, d);
            return d;
        }

        /// <summary>
        /// Schreibt die KONFIGURATIONSFELDER der Anlage zurück — ein schmales UPDATE
        /// auf <c>Tab_Energieanlagen</c> über <c>WErzeugerCtrl.KonfigurationSchreiben</c>.
        /// </summary>
        /// <remarks>
        /// <para><b>Was hier bis zum 16.09.2026 stand</b>, war der Bestandsweg der
        /// Erzeuger: <c>NachModell</c> in die Zeile, dann
        /// <c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> + <c>Add_WP_Waermeerzeuger</c>
        /// für die ganze Anlagenliste des Projekts. Er war nicht falsch, aber zu teuer
        /// und hatte eine sichtbare Folge: <b>Löschen und Neuanlegen vergibt neue
        /// Anlagen-Ids.</b> Die Karte, die man gerade bearbeitet hatte, war danach nicht
        /// mehr markiert, und jede Zuordnung, die an der alten Id hängt, muss beim
        /// Neuanlegen gerettet werden. Für acht Felder ist das der falsche Preis.</para>
        ///
        /// <para><b>Warum es jetzt geht.</b> Der Grund für den Umweg war
        /// <c>WErzeugerCtrl.Update</c>: Dessen UPDATE führt sechzehn Spalten und darunter
        /// weder <c>Heizstab</c> noch <c>ID_Carrier</c> — die zwei Felder, um die es hier
        /// vor allem geht. <c>WErzeugerCtrl.KonfigurationSchreiben</c> ist genau für
        /// diesen Satz gebaut: acht Felder, read-modify-write (<c>null</c> = unverändert),
        /// adressiert über <c>ID</c> UND <c>ID_Projekt</c>, die Ids bleiben stehen.</para>
        ///
        /// <para><b>Nur die KONFIGURATION, nicht der ganze Feldsatz.</b> Der Dialog an
        /// der Karte zeigt den Baustein <c>WaermepumpeKonfiguration</c> und nichts sonst;
        /// Vorlauf, Rücklauf, Nutzungsdauer und die Stammfelder bearbeitet allein der
        /// Anlagendialog, und der geht weiter seinen eigenen Speicherweg. Ein Schreibweg,
        /// der hier mehr anfasste, als der Dialog zeigt, schriebe Werte zurück, die
        /// niemand angesehen hat.</para>
        /// </remarks>
        internal static AnlagenkonfigErgebnis Schreiben(
            int idProjekt, int idAnlage, WaermepumpeAnlageDaten daten)
        {
            if (daten == null)
                return new AnlagenkonfigErgebnis(false, Text_("ANL_KONFIG_MSG_FEHLER",
                    "Die Konfiguration der Anlage konnte nicht gespeichert werden."));

            WErzeugerCtrl.SpeicherErgebnis e = WErzeugerCtrl.KonfigurationSchreiben(
                idAnlage, idProjekt,
                BivalenzAbbildung.MitUebergabe(BetriebszeitenAbbildung.MitBetriebszeiten(new WErzeugerCtrl.KonfigurationFelder(
                    Heizstab: daten.Heizstab,
                    // V14: Mit Fensterliste ist das Altfenster in die Liste ueberfuehrt.
                    Sperrung: daten.Sperrfenster is null && daten.Sperrung,
                    SperrzeitVon: daten.SperrzeitVon ?? 0,
                    SperrzeitBis: daten.SperrzeitBis ?? 0,
                    BivalenterBetrieb: daten.BivalenterBetrieb,
                    Betriebsart: daten.Betriebsart ?? "",
                    Abschaltpunkt: daten.Abschaltpunkt,
                    // 0 heisst hier "nicht anfassen", nicht "kein Traeger": Der Dialog
                    // bietet keine Moeglichkeit, die Wahl ZURUECKZUNEHMEN - er laesst
                    // sie nur unberuehrt, solange die Anlage noch keine fuehrt.
                    IdCarrier: daten.CarrierId > 0 ? daten.CarrierId : (int?)null,
                    // KU2 Welle 3 (E33, E34): Kuehltraeger (0 = wie Heizbetrieb, NULL) und
                    // Abrechnungsart (false = anteilig, NULL) - gelesen mit der Zeile, also
                    // unveraendert, wenn niemand sie angefasst hat.
                    KuehlIdCarrier: daten.KuehlCarrierId ?? 0,
                    KuehlEigenerZaehler: daten.KuehlEigenerZaehler == true,
                    // KU3-6 (F1): die drei Anlagenfelder der freien Kuehlung ueber die Waermequelle -
                    // der Gruppenschalter sagt dem Kern, dass sie geschrieben werden sollen.
                    FreieKuehlung: true,
                    KuehlFrei: daten.KuehlFrei,
                    KuehlFreiGraedigkeitK: daten.KuehlFreiGraedigkeitK,
                    KuehlFreiLeistungKw: daten.KuehlFreiLeistungKw), daten), daten));

            // ET-5: der gewaehlte Traeger gehoert dem Projekt zugeordnet. Idempotent;
            // er steht auch dann an, wenn der Satz sonst unveraendert blieb.
            if (e.Ok) ErzeugerTraegerHuelle.Zuordnen(idProjekt, false, daten.CarrierId);

            // Uebergabegrenze UB-E2 (U-2): die Kaeltemittelwahl gehoert dem Geraet (Projektkopie).
            if (e.Ok) BivalenzAbbildung.GeraetSchreiben(daten);

            // V14: die Sperrfenster der Anlage - die Liste ersetzt den Bestand.
            if (e.Ok && daten.Sperrfenster is not null &&
                !SperrfensterCtrl.Schreiben(idAnlage, SperrfensterAbbildung.Fenster(daten.Sperrfenster)))
                return new AnlagenkonfigErgebnis(false, Text_("ANL_KONFIG_MSG_FEHLER",
                    "Die Konfiguration der Anlage konnte nicht gespeichert werden."));

            // KU2 Welle 3 (Kuehlkonzept 8.2): die drei Geraetefelder des Kuehlbetriebs in die
            // Projektkopie - ueber den einen Schreibweg des Kerns samt Sperrgruenden, nur wenn
            // sie sich geaendert haben. Ein abgelehnter Kuehlbetrieb meldet seinen Grund.
            if (e.Ok)
            {
                string kuehlGrund = WaermepumpeGeraeteCtrl.KuehlkonfigurationNachziehen(
                    daten.IdWp, idProjekt, daten.Kuehlbetrieb, daten.KuehlVorlauf, daten.KuehlHilfsstromanteil);
                if (kuehlGrund != null) return new AnlagenkonfigErgebnis(false, kuehlGrund);
            }

            return new AnlagenkonfigErgebnis(e.Ok, e.Meldung ?? "");
        }

        /// <summary>
        /// Die Anlagenzeile zu einer Id — der LESEWEG braucht sie samt der Liste, in der sie steht; der
        /// Schreibweg adressiert unmittelbar über (<c>ID</c>, <c>ID_Projekt</c>).
        /// </summary>
        private static WErzeugerModel Anlage(int idProjekt, int idAnlage, out List<WErzeugerModel> modelle)
        {
            modelle = WErzeugerCtrl.ModelleJeTyp(idProjekt, WizardItemClass.WP_TYP);

            foreach (WErzeugerModel m in modelle)
                if (m.ID == idAnlage) return m;

            return null;
        }

        // =================================================================================
        //  Der Parametersatz der Anlagenfelder (KM‑K2)
        // =================================================================================

        /// <summary>
        /// <b>Der Bearbeitungsweg einer Projektwärmepumpe</b> (KM‑K2): Feldsatz, Gaben und Schreibweg der
        /// ERSTEN Anlagenzeile dieser Projektkopie (<c>Tab_WP.ID</c>) — dieselbe Reihenfolge, in der die
        /// Projektliste des Kältemaschinen-Dialogs ihre Anlagen zählt. <c>null</c> = die Kopie führt im
        /// Projekt keine Anlagenzeile; der Dialog bleibt dann bei seiner benannten Sperre.
        /// </summary>
        internal static WaermepumpeAnlagegaben Gaben(int idProjekt, int idWp)
        {
            if (idProjekt <= 0 || idWp <= 0) return null;
            List<int> anlagen = KuehlerzeugerKatalogCtrl.AnlagenDerKopie(idProjekt, idWp);
            if (anlagen.Count == 0) return null;
            int idAnlage = anlagen[0];
            WaermepumpeAnlageDaten daten = Lesen(idProjekt, idAnlage);
            if (daten == null) return null;
            WErzeugerModel modell = Anlage(idProjekt, idAnlage, out _);

            var gaben = new Dictionary<string, object>
            {
                ["Daten"] = daten,
                // Die Zeile „Senken: …" der Anlage, fertig formuliert im Kern (wie im Wärmepumpendialog).
                ["SenkenText"] = Senkenvorbelegung.Anzeigezeile(idProjekt, idAnlage, WizardItemClass.WP_TYP),
                ["Traegerkatalog"] = ErzeugerTraegerHuelle.Katalog(DbWerte.ERZEUGER_WAERMEPUMPE),
                // KU2 Welle 3 (Kühlkonzept 8.2): die Klappgruppe „Kühlbetrieb" samt Sperrgrund und
                // Stützstellen; mit der Quelle der Anlagenzeile trägt sie den Grund der freien Kühlung.
                ["Kuehlung"] = WaermepumpeKuehlGabenBau.Bauen(idProjekt, modell != null ? (modell.WQ_Typ ?? "") : null),
                ["Vorlaeufe"] = new Func<int, IReadOnlyList<int>>(WaermepumpeKennlinienCtrl.VorlaeufeFuerAnlage),
                ["ProjektkopieVorhanden"] = new Func<int, bool>(id => WPCtrl.ProjektgeraetVorhanden(id, idProjekt)),
                // WP1a: der Arbeitspunkt am eingestellten Vorlauf für den Gerätesteckbrief.
                ["Arbeitspunkt"] = new Func<int, int, string, WaermepumpeArbeitspunkt>(
                    (id, vorlauf, typ) => Kennlinienarbeitspunkt.Rechnen(id, vorlauf, typ) is KennlinienArbeitspunkt a
                        ? new WaermepumpeArbeitspunkt(a.Normpunkt, a.Heizleistung, a.Cop)
                        : null),
                ["TemperaturenPruefen"] = new Func<int?, int?, string>(TemperaturenPruefen),
                ["RuecklaufVorbelegen"] = new Action<WaermepumpeAnlageDaten>(d => TemperaturVorbelegung.Waermepumpe(d)),
                ["ExtrapolationErlaubt"] = KonfigurationCtrl.ExtrapolationErlaubtLesen(idProjekt),
                ["ExtrapolationSchreiben"] = new Func<bool, bool>(
                    wert => KonfigurationCtrl.ExtrapolationErlaubtSchreiben(idProjekt, wert))
            };
            TexteEinsetzen(gaben);

            return new WaermepumpeAnlagegaben(idAnlage, daten.Bezeichner ?? "", gaben, () => Fehlertext(Schreiben(idProjekt, idAnlage, daten)));
        }

        /// <summary>Der Grund einer Ablehnung; <c>null</c> = geschrieben.</summary>
        private static string Fehlertext(AnlagenkonfigErgebnis e)
        {
            if (e != null && e.Ok) return null;
            string grund = e == null ? "" : e.Meldung ?? "";
            return grund.Length > 0 ? grund : Text_("ANL_KONFIG_MSG_FEHLER",
                "Die Konfiguration der Anlage konnte nicht gespeichert werden.");
        }

        /// <summary>
        /// Die Beschriftungen der Anlagenfelder aus den Ressourcen — dieselben Schlüssel, die die
        /// Windows-Hülle setzt; ohne sie zeigte die Komponente ihre deutschen Rückfalltexte auch auf Englisch.
        /// </summary>
        private static void TexteEinsetzen(Dictionary<string, object> gaben)
        {
            gaben["GruppeKenndaten"] = Text_("WPA_GRP_KENNDATEN", "Wärmepumpen Kenndaten");
            gaben["GruppeAuslegung"] = Text_("WPA_GRP_AUSLEGUNG", "Auslegung für Verteilung");
            gaben["GruppeKonfiguration"] = Text_("WPA_GRP_KONFIGURATION", "Konfiguration");
            gaben["BtnKonfigurationText"] = Text_("WPA_BTN_KONFIGURATION", "Konfiguration…");
            gaben["LabelPHeizstabKurz"] = Text_("WPA_LBL_PHEIZSTAB", "Leistung Heizstab");
            gaben["LabelVorlauf"] = Text_("WPA_LBL_VORLAUF", "Vorlauf");
            gaben["LabelRuecklauf"] = Text_("WPA_LBL_RUECKLAUF", "Rücklauf");
            gaben["LabelRuecklaufKurz"] = Text_("WPA_LBL_RUECKLAUF", "Rücklauf");
            gaben["LabelKennlinien"] = Text_("WPS_LBL_KENNLINIEN", "Kenndaten Kennlinien:");
            gaben["RuecklaufFormat"] = Text_("WPA_RUECKLAUF_VORSCHLAG", "Übliche Werte: {0} °C");
            gaben["PlatzhalterBild"] = Text_("WPS_PLATZHALTER_BILD", "Keine Kennlinien vorhanden");
            gaben["ReiterCop"] = Text_("WPS_REITER_COP", "COP");
            gaben["ReiterLeistung"] = Text_("WPS_REITER_LEISTUNG", "Leistung");
            gaben["HerleitungKatalog"] = Text_("WPA_HERLEITUNG_KATALOG", "");
            gaben["HerleitungKuehlKatalog"] = Text_("WPA_HERLEITUNG_KUEHL_KATALOG", "");
            gaben["LabelExtrapolation"] = MyResource.Resource.SIM_EXTRAPOLATION_SCHALTER;
            gaben["HinweisExtrapolation"] = Text_("WPA_HINWEIS_EXTRAPOLATION",
                "Projekteinstellung — gilt für alle Wärmepumpen des Projekts und wird sofort gespeichert.");
            gaben["WarnungExtrapolation"] = Text_("WPA_MSG_EXTRAPOLATION_FEHLER",
                "Die Einstellung „Extrapolation der WP-Kennlinie erlauben“ ließ sich nicht speichern.");
            gaben["WarnungBetriebsart"] = Text_("WPA_MSG_BETRIEBSART", "Bitte Betriebsart auswählen!");
            gaben["WarnungWaermepumpe"] = Text_("WPA_MSG_WAERMEPUMPE", "Bitte Wärmepumpe auswählen!");
            gaben["WarnungFeldFormat"] = Text_("WPA_MSG_FELD", "Bitte {0} eingeben.");
            gaben["SchliessenText"] = Text_("WPV_BTN_SCHLIESSEN", "Schließen");
            gaben["OkText"] = MyResource.Resource.ALLG_BTN_OK;
            gaben["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN;
        }

        /// <summary>
        /// Die Prüfung aus <c>ProjektPuffer.TemperaturenPruefen</c> — sie kommt als Delegat in die
        /// Komponente, weil die Klasse im Kern <c>internal</c> ist.
        /// </summary>
        private static string TemperaturenPruefen(int? vorlauf, int? ruecklauf)
        {
            int v, r;
            string fehler;
            bool ok = ProjektPuffer.TemperaturenPruefen(
                vorlauf?.ToString() ?? "", ruecklauf?.ToString() ?? "", out v, out r, out fehler);
            return ok ? null : fehler;
        }

        // =================================================================================
        //  Abbildung
        // =================================================================================

        /// <summary>Aus der Anlagenzeile in den Feldsatz — <c>SetControls</c>:151.</summary>
        internal static WaermepumpeAnlageDaten AusModell(WErzeugerModel m)
        {
            // KU2 Welle 3: die drei Geraetefelder des Kuehlbetriebs, falls der Aufrufer die
            // Zeile ohne GeraetedatenFuellen hereinreicht - sonst zeigte der Dialog "aus",
            // und der Speicherweg liesse den gespeicherten Stand ohnehin stehen.
            WaermepumpeGeraeteCtrl.KuehlfelderFuellen(m);

            var d = new WaermepumpeAnlageDaten
            {
                Bezeichner = m.Bezeichner ?? "",
                IdWp = m.ID_WP,
                Vorlauf = m.Vorlauf,
                Ruecklauf = m.Ruecklauf,
                Heizstab = m.Heizstab,
                HeizstabLeistung = (int)m.Heizung,
                Sperrung = m.Sperrung,
                SperrzeitVon = m.Sperrzeit_von,
                SperrzeitBis = m.Sperrzeit_bis,
                // V14: die Sperrfenster samt aktivem Altfenster als erste Zeile.
                Sperrfenster = SperrfensterAbbildung.Lesen(m.ID, m.Sperrung, m.Sperrzeit_von, m.Sperrzeit_bis),
                // Die Nutzungsdauer (Tab_Energieanlagen.Nutzungszeit) fuehrt der Feldsatz
                // nicht mehr (Anwenderauftrag 30.09.2026) - der Wert bleibt am Modell, wie
                // er gelesen wurde, und reist mit Loeschen + Neuanlegen unveraendert mit.
                BivalenterBetrieb = m.Bivalenter_Betrieb,
                CarrierId = m.ID_Carrier,

                // W7-B-2 (Windows-Abnahme 06.09.2026): TOLERANT lesen. Der
                // Vorlaeufer Wizard_WPItem hatte eine frei beschreibbare ComboBox
                // und schrieb ihren Text ungeprueft in die Spalte (Befund L0-1);
                // ein <select> kann einen nicht zeichengleichen Wert gar nicht
                // zeigen - die Klappliste stand leer und der OK-Knopf meldete
                // "Bitte Betriebsart auswaehlen!". Geschrieben wird der
                // berichtigte Wert erst beim OK; die Engine vergleicht
                // unveraendert zeichengleich, der Referenzlauf bleibt unberuehrt.
                Betriebsart = DbWerte.BetriebsartOderDefault(m.Betriebsart),
                Abschaltpunkt = m.Abschaltpunkt,
                Beschreibung = m.Beschreibung ?? "",
                Baujahr = m.Baujahr,
                Regelung = m.Regelung ?? "",
                Typ = m.Typ ?? "",

                // 16.09.2026: Die Aufstellungsart gehoert seit dem Anwenderentscheid zum
                // Feldsatz - die Stammfelder des Dialogs bearbeiten die Projektkopie, und
                // Tab_WP.Aufstellung ist eine ihrer Spalten.
                Aufstellung = m.Aufstellung ?? "",
                Firma = m.Firma ?? "",
                Nennleistung = m.Nennleistung,

                // 16.09.2026: Die Kuehlleistung der PROJEKTKOPIE steht seither im
                // Feldsatz und ist im Stammfeldblock bearbeitbar - wie die
                // Nennleistung daneben. Sie ist eine Kommazahl (Tab_WP.Kuehlleistung
                // ist REAL); ein int schnitte 5,5 kW still auf 5 kW.
                Kuehlleistung = m.Kuehlleistung,

                // KU2 Welle 3 (Kuehlkonzept 8.2; E15, E33, E34): der Kuehlbetrieb - drei Felder
                // der Projektkopie, zwei der Anlagenzeile.
                Kuehlbetrieb = m.Kuehlbetrieb,
                KuehlVorlauf = m.KuehlVorlauf,
                KuehlHilfsstromanteil = m.KuehlHilfsstromanteil,
                KuehlCarrierId = m.Kuehl_ID_Carrier,
                KuehlEigenerZaehler = m.Kuehl_EigenerZaehler,

                // KU3-6 (F1): die freie Kuehlung ueber die Waermequelle - drei Felder der Anlagenzeile.
                KuehlFrei = m.Kuehl_Frei,
                KuehlFreiGraedigkeitK = m.Kuehl_Frei_Graedigkeit_K,
                KuehlFreiLeistungKw = m.Kuehl_Frei_Leistung_kW,

                Modulkosten = m.Modulkosten,
                Volumen = m.Volumen,
                Solaranteil = m.Solaranteil,
                RendeMix = m.rendeMix
            };

            // Welle M4 (WP1): Mindestleistung und C_d der Projektkopie - nur zur Anzeige; sie reisen
            // mit der Übernahme aus dem Katalog und werden hier nicht zurückgeschrieben.
            WPModel geraet = m.ID_WP > 0 ? WaermepumpeGeraeteCtrl.Geraetedaten(m.ID_WP) : null;
            if (geraet != null)
            {
                d.MindestleistungKw = geraet.MindestleistungKw;
                d.TaktverlustfaktorCd = geraet.TaktverlustfaktorCd;
            }

            // Anwenderauftrag 30.09.2026: ein Ruecklauf 0/leer wird aus dem Vorlauf
            // vorbelegt (Kern-Regel ueber TemperaturVorbelegung) - im FELDSATZ; ins Modell
            // kommt er erst mit dem OK (NachModell).
            // Anlagenkopplung AK2 (9.3): Zeitprogramm und hoechster Vorlauf, NULL-erhaltend.
            BetriebszeitenAbbildung.Lesen(m, d);
            // Uebergabegrenze UB-E2: Einbindung und Vorwaermbetrieb, leer bleibt leer (U-1).
            BivalenzAbbildung.AnlageLesen(m, d);
            TemperaturVorbelegung.Waermepumpe(d);
            return d;
        }

        /// <summary>Ressourcentext mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
