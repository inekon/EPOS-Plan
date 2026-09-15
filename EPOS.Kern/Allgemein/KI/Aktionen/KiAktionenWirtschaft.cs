using System;
using System.Collections.Generic;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Leseaktionen der Wirtschaftlichkeit und der Kostenlage
    /// (Fachkonzept 5.1, Zeilen 5-7).
    /// </summary>
    internal static class KiAktionenWirtschaft
    {
        // =====================================================================
        // ergebnisse_lesen
        // =====================================================================

        /// <summary>
        /// Gespeicherte Wirtschaftlichkeitsergebnisse. Andockpunkt
        /// <c>WirtschaftlichkeitCtrl.LadeErgebnisse(List&lt;int&gt;)</c>, Aktualitaet ueber
        /// <c>ErgebnisAktuell(WirtschaftlichkeitErgebnis)</c>.
        /// </summary>
        internal static KiAktion ErgebnisseLesen()
        {
            return new KiAktion(
                name: "ergebnisse_lesen",
                zweck: KiAktionsTexte.ZweckErgebnisseLesen,
                titel: KiAktionsTexte.TitelErgebnisseLesen,
                beispiel: KiAktionsTexte.BeispielErgebnisseLesen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "WirtschaftlichkeitCtrl.LadeErgebnisse / ErgebnisAktuell",
                parameter: new[]
                {
                    new KiParameter("projekte", KiParameterTyp.Text,
                                    KiAktionsTexte.ErlProjekteFuerErgebnisse,
                                    anzeigename: KiAktionsTexte.ProjekteName, maxLaenge: 600)
                },
                ausfuehren: a =>
                {
                    string ungeklaert;
                    var ids = KiHilfe.ProjektIds(a, "projekte", out ungeklaert);
                    if (ids.Count == 0) return KiErgebnis.Fehlgeschlagen(ungeklaert);
                    var ctrl = new WirtschaftlichkeitCtrl();
                    List<WirtschaftlichkeitErgebnis> ergebnisse = ctrl.LadeErgebnisse(ids);

                    var zeilen = KiHilfe.Liste();
                    int aktuell = 0;
                    foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
                    {
                        bool istAktuell = ctrl.ErgebnisAktuell(e);
                        if (istAktuell) aktuell++;

                        zeilen.Add(KiHilfe.Zeile(
                            "id_projekt", e.IdProjekt,
                            "anzeige", KiHilfe.Text(e.Anzeige),
                            "szenario", KiHilfe.Text(e.Szenario),
                            "ist_stamm", e.IstStamm,
                            "investition_eur", KiHilfe.Wert(e.Investition),
                            "kapitalwert_eur", KiHilfe.Wert(e.Kapitalwert),
                            "kapitalwert_diff_eur", KiHilfe.Wert(e.KapitalwertDiff),
                            "amortisation_a", KiHilfe.Wert(e.AmortisationJahre),
                            "gestehungskosten_eur_kwh", KiHilfe.Wert(e.Gestehungskosten),
                            "aktuell", istAktuell,
                            "fehlgrund", KiHilfe.Text(e.Fehlgrund)));
                    }

                    if (zeilen.Count == 0)
                        return KiErgebnis.Ok(KiAktionsTexte.ErgebnisseKeine);

                    return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.ErgebnisseGefunden,
                                                       zeilen.Count, ids.Count, aktuell),
                                         zeilen);
                });
        }

        // =====================================================================
        // wirtschaftlichkeit_parameter_lesen
        // =====================================================================

        /// <summary>
        /// Parametersatz und Stromtarif. Andockpunkt
        /// <c>WirtschaftlichkeitCtrl.LadeParameter(int)</c> und <c>LadeTarif(int)</c>.
        /// </summary>
        internal static KiAktion ParameterLesen()
        {
            return new KiAktion(
                name: "wirtschaftlichkeit_parameter_lesen",
                zweck: KiAktionsTexte.ZweckParameterLesen,
                titel: KiAktionsTexte.TitelParameterLesen,
                beispiel: KiAktionsTexte.BeispielParameterLesen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "WirtschaftlichkeitCtrl.LadeParameter / LadeTarif",
                parameter: new[] { KiHilfe.ProjektParameter() },
                vorbedingung: a => KiHilfe.ProjektMussAufloesbarSein(a),
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    var ctrl = new WirtschaftlichkeitCtrl();

                    WirtschaftlichkeitParameter p = ctrl.LadeParameter(id);
                    TarifParameter t = ctrl.LadeTarif(id);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_projekt", p.IdStamm,
                        "zinssatz_prozent", KiHilfe.Wert(p.Zinssatz),
                        "betrachtungszeitraum_a", p.Betrachtungszeitraum,
                        "preissteigerung_energie_prozent", KiHilfe.Wert(p.PreissteigerungEnergie),
                        "preissteigerung_betrieb_prozent", KiHilfe.Wert(p.PreissteigerungBetrieb),
                        // ETAPPE W5-B-12 (Anwenderentscheid 09.09.2026): p_I - der Satz,
                        // mit dem die Ersatzbeschaffungen fortgeschrieben werden. Gemeldet
                        // wird der WIRKSAME Wert samt Herkunft: Ein leeres Feld heisst
                        // "wie p_B" und nicht "0 %/a", und ohne die Herkunft koennte der
                        // Assistent beides nicht auseinanderhalten.
                        "preissteigerung_investition_prozent", KiHilfe.Wert(p.PreisInvestWirksam),
                        "preissteigerung_investition_herkunft",
                        p.PreissteigerungInvestition.HasValue ? "gepflegt" : "wie_betrieb",
                        "nicht_monetaere_wirkungen", KiHilfe.Text(p.NichtMonetaer),
                        "einspeiseverguetung_eur_kwh", KiHilfe.Wert(p.Einspeiseverguetung),
                        "co2_preis_eur_t", KiHilfe.Wert(p.CO2Preis),
                        "kwkg_bonus_ct_kwh", KiHilfe.Wert(p.KwkgBonus),
                        "id_kraftwerkspark", p.IdKraftwerkspark,
                        "refkessel_wirkungsgrad_prozent", KiHilfe.Wert(p.RefKesselWirkungsgrad),
                        "tarif_aktiv", t.Aktiv,
                        "tarif_winter_von_monat", t.WinterVonMonat,
                        "tarif_winter_bis_monat", t.WinterBisMonat,
                        "tarif_ht_von_stunde", t.HtVonStunde,
                        "tarif_ht_bis_stunde", t.HtBisStunde));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.ParameterGelesen, id,
                                      t.Aktiv ? KiAktionsTexte.TarifAktiv : KiAktionsTexte.TarifAus),
                        zeilen);
                });
        }

        // =====================================================================
        // kostenlage_pruefen
        // =====================================================================

        /// <summary>
        /// Vergleich der erfassten Investitionsposition mit den Technik-Planwerten.
        /// Andockpunkt <c>KostenPositionCtrl.Pruefe(int, string, int, int)</c>; Anlagen ueber
        /// <c>TechnikPlanwertCtrl.LiesAnlagen(int, string)</c>.
        /// </summary>
        /// <remarks>
        /// Die zulaessigen Komponentennamen sind PERSISTENZWERTE aus <see cref="DbWerte"/> -
        /// sie stehen so in <c>Tab_KostenKomponente.Komponente</c> und werden in SQL damit
        /// verglichen. Ob eine Komponente ueberhaupt Technikdaten fuehrt, entscheidet
        /// <c>TechnikPlanwertCtrl.Bekannt</c> - dieselbe Quelle wie im Bestand.
        /// </remarks>
        internal static KiAktion KostenlagePruefen()
        {
            return new KiAktion(
                name: "kostenlage_pruefen",
                zweck: KiAktionsTexte.ZweckKostenlagePruefen,
                titel: KiAktionsTexte.TitelKostenlagePruefen,
                beispiel: KiAktionsTexte.BeispielKostenlagePruefen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KostenPositionCtrl.Pruefe / TechnikPlanwertCtrl.LiesAnlagen",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(),
                    new KiParameter("komponente", KiParameterTyp.Aufzaehlung,
                                    KiAktionsTexte.ErlKomponente,
                                    anzeigename: KiAktionsTexte.KomponenteName,
                                    werte: Komponenten)
                },
                vorbedingung: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    string komponente = a.Text("komponente");

                    string grund = KiHilfe.ProjektMussAufloesbarSein(a);
                    if (grund != null) return grund;

                    if (!TechnikPlanwertCtrl.Bekannt(komponente))
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.KomponenteUnbekannt, komponente);

                    if (!TechnikPlanwertCtrl.Verbaut(id, komponente))
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.KomponenteNichtVerbaut, id, komponente);

                    return null;
                },
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    string komponente = a.Text("komponente");

                    int komponentenId = KomponentenId(komponente);
                    if (komponentenId <= 0)
                        return KiErgebnis.Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                                                  KiAktionsTexte.KostenlageOhneKomponente,
                                                                  komponente));

                    KostenPositionCtrl.Abweichung ab = KostenPositionCtrl.Pruefe(
                        id, komponente, DbWerte.KOSTEN_KATEGORIE_INVESTITION, komponentenId);

                    List<TechnikPlanwertCtrl.Anlage> anlagen = TechnikPlanwertCtrl.LiesAnlagen(id, komponente);

                    // Die ID der Hauptposition gehoert seit Etappe 3 in das Ergebnis:
                    // Ohne sie koennte kostenposition_setzen nicht angesteuert werden,
                    // ohne dass das Modell eine ID erfindet.
                    int idPosition = KostenPositionCtrl.FindeHauptposition(
                        id, DbWerte.KOSTEN_KATEGORIE_INVESTITION, komponentenId, komponente);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_projekt", id,
                        "komponente", komponente,
                        "id_position", idPosition,
                        "erfasst_eur", KiHilfe.Wert(ab.Erfasst),
                        "technik_eur", KiHilfe.Wert(ab.Technik),
                        "technik_vorhanden", ab.TechnikVorhanden,
                        "auswahl_offen", ab.AuswahlOffen,
                        "abweichend", ab.Abweichend,
                        "anlagen", anlagen.Count,
                        "hinweis", KiHilfe.Text(ab.Text)));

                    string text = ab.Abweichend
                        ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.KostenlageAbweichend, komponente)
                        : string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.KostenlagePasst, komponente);

                    return KiErgebnis.Ok(text, zeilen, anzahl: 1);
                });
        }

        /// <summary>
        /// Die sieben Kostenkomponenten - Persistenzwerte aus <see cref="DbWerte"/>
        /// (Drei-Schichten-Regel), in derselben Auswahl wie
        /// <c>TechnikPlanwertCtrl.Plaene</c>.
        /// </summary>
        private static readonly string[] Komponenten =
        {
            DbWerte.ERZEUGER_WAERMEPUMPE,
            DbWerte.ERZEUGER_HEIZKESSEL,
            DbWerte.ERZEUGER_PHOTOVOLTAIK,
            DbWerte.ERZEUGER_SOLARTHERMIE,
            DbWerte.ERZEUGER_STROMSPEICHER,
            DbWerte.KOSTEN_KOMPONENTE_PUFFERSPEICHER,
            DbWerte.ERZEUGER_BHKW
        };

        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> einer Komponente - dieselbe Abfrage wie in
        /// <c>UcBkKosten.Abweichung</c> und <c>KomponentenUebernahmeCtrl.KostenabweichungMelden</c>.
        /// </summary>
        private static int KomponentenId(string komponente)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT MIN(ID) FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new DbParam("@k", komponente ?? ""));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        // =====================================================================
        // wirtschaftlichkeit_parameter_setzen  (Fachkonzept 5.2, 15.09.2026)
        // =====================================================================

        /// <summary>
        /// Aendert einzelne Parameter der Wirtschaftlichkeitsrechnung. Andockpunkt
        /// <c>WirtschaftlichkeitCtrl.LadeParameter</c> + <c>SpeichereParameter</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TEILAENDERUNG, kein Parametersatz.</b> Der Bestand speichert immer den
        /// GANZEN Satz; diese Aktion laedt ihn deshalb, ueberschreibt nur die genannten
        /// Felder und speichert ihn zurueck. Ein Aufruf mit drei Werten laesst die uebrigen
        /// acht nachweislich unberuehrt - haette die Aktion dagegen einen vollstaendigen
        /// Satz verlangt, muesste das Modell die anderen acht MITLIEFERN, und jeder
        /// vergessene Wert waere eine stille Nullsetzung.
        /// </para>
        /// <para>
        /// <b>Sieben Felder, nicht elf.</b> Aufgenommen ist, wonach im Gespraech gefragt
        /// wird. <c>id_kraftwerkspark</c> und <c>nicht_monetaere_wirkungen</c> bleiben
        /// aussen vor (ein Verweis in eine Katalogliste bzw. ein Freitext), ebenso der
        /// Tarifsatz - der haengt an <c>SpeichereTarif</c> und damit an einer zweiten
        /// Schreibmethode; er bekaeme eine eigene Aktion mit eigener Vorschau.
        /// </para>
        /// <para>
        /// <b>UMKEHRBAR:</b> Der Vorzustand jedes geaenderten Feldes steht in der
        /// Vorschau und im Ergebnis - Zurueckschreiben ist derselbe Aufruf mit den alten
        /// Zahlen, mit eigener Bestaetigung.
        /// </para>
        /// <para>
        /// <b>Nur am STAMMPROJEKT.</b> Der Parametersatz haengt an <c>IdStamm</c>; an
        /// einer Variante gaebe es ihn gar nicht, und <c>SpeichereParameter</c> legte
        /// stillschweigend einen zweiten an.
        /// </para>
        /// </remarks>
        internal static KiAktion ParameterSetzen()
        {
            return new KiAktion(
                name: "wirtschaftlichkeit_parameter_setzen",
                zweck: KiAktionsTexte.ZweckParameterSetzen,
                titel: KiAktionsTexte.TitelParameterSetzen,
                beispiel: KiAktionsTexte.BeispielParameterSetzen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "WirtschaftlichkeitCtrl.SpeichereParameter",
                wirkung: KiAktionsTexte.WirkungParameterSetzen,
                umkehrbar: true,
                parameter: Parameterfelder(),
                vorbedingung: a =>
                {
                    string grund = KiHilfe.ProjektMussAufloesbarSein(a);
                    if (grund != null) return grund;

                    int id = KiHilfe.ProjektId(a);

                    int stammRef = new VariantenCtrl().StammRefDerVariante(id);
                    if (stammRef > 0)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.ParameterNurAmStamm, id, stammRef);

                    if (Genannte(a).Count == 0) return KiAktionsTexte.ParameterOhneAngabe;

                    return KiSchreibschutz.Gesperrt("Tab_Projekt", "ID", id);
                },
                vorschau: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(id);

                    var aenderungen = new List<KiFeldAenderung>();
                    foreach (Feld f in Genannte(a))
                        aenderungen.Add(new KiFeldAenderung(
                            f.Anzeigename,
                            Zahltext(f.Lesen(p), f.Einheit),
                            Zahltext(a.Zahl(f.Name), f.Einheit)));

                    return KiFeldBlock.Felder(KiHilfe.ProjektName(id), aenderungen);
                },
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    var ctrl = new WirtschaftlichkeitCtrl();

                    // ERST laden, DANN ueberschreiben: Alles, was der Aufruf nicht nennt,
                    // geht unveraendert wieder hinaus.
                    WirtschaftlichkeitParameter p = ctrl.LadeParameter(id);

                    var zeilen = KiHilfe.Liste();
                    foreach (Feld f in Genannte(a))
                    {
                        double vorher = f.Lesen(p);
                        double neu = a.Zahl(f.Name);
                        f.Setzen(p, neu);

                        zeilen.Add(KiHilfe.Zeile(
                            "id_projekt", id,
                            "feld", f.Name,
                            "anzeigename", KiHilfe.Text(f.Anzeigename),
                            "einheit", KiHilfe.Text(f.Einheit),
                            "wert_vorher", KiHilfe.Wert(vorher),
                            "wert_nachher", KiHilfe.Wert(neu)));
                    }

                    if (!ctrl.SpeichereParameter(p))
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          KiAktionsTexte.ParameterFehlgeschlagen, id));

                    string datumsmeldung = KiAktionenSchreiben.AenderungsdatumSetzen(id);

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.ParameterGesetzt,
                                      zeilen.Count, KiHilfe.ProjektName(id)),
                        zeilen, anzahl: zeilen.Count);

                    if (datumsmeldung != null) e.MitMeldungen(new[] { datumsmeldung });
                    return e;
                });
        }

        /// <summary>
        /// EIN aenderbares Parameterfeld: Name fuer das Modell, Klartext fuer die
        /// Bestaetigung und die zwei Zugriffe auf den Parametersatz.
        /// </summary>
        /// <remarks>
        /// Eine Deklaration, vier Verwendungen - Werkzeugkatalog, Vorbedingung, Vorschau
        /// und Lauf lesen alle aus dieser Tabelle. Eine zweite Liste („welche Felder gibt
        /// es") waere genau die, die man beim Nachtragen vergisst.
        /// </remarks>
        private sealed class Feld
        {
            internal string Name;
            internal string Anzeigename;
            internal string Erlaeuterung;
            internal string Einheit;
            internal double Min;
            internal double Max;
            internal Func<WirtschaftlichkeitParameter, double> Lesen;
            internal Action<WirtschaftlichkeitParameter, double> Setzen;
        }

        /// <summary>Die sieben aenderbaren Felder - die EINE Tabelle.</summary>
        private static readonly Feld[] FELDER =
        {
            new Feld { Name = "zinssatz_prozent", Anzeigename = KiAktionsTexte.FeldZinssatz,
                       Erlaeuterung = KiAktionsTexte.ErlZinssatz,
                       Einheit = "%", Min = 0, Max = 100,
                       Lesen = p => p.Zinssatz, Setzen = (p, w) => p.Zinssatz = w },

            new Feld { Name = "betrachtungszeitraum_a", Anzeigename = KiAktionsTexte.FeldZeitraum,
                       Erlaeuterung = KiAktionsTexte.ErlZeitraum,
                       Einheit = "a", Min = 1, Max = 100,
                       Lesen = p => p.Betrachtungszeitraum,
                       Setzen = (p, w) => p.Betrachtungszeitraum = (int)Math.Round(w) },

            new Feld { Name = "preissteigerung_energie_prozent", Anzeigename = KiAktionsTexte.FeldPreisEnergie,
                       Erlaeuterung = KiAktionsTexte.ErlPreisEnergie,
                       Einheit = "%/a", Min = -50, Max = 100,
                       Lesen = p => p.PreissteigerungEnergie,
                       Setzen = (p, w) => p.PreissteigerungEnergie = w },

            new Feld { Name = "preissteigerung_betrieb_prozent", Anzeigename = KiAktionsTexte.FeldPreisBetrieb,
                       Erlaeuterung = KiAktionsTexte.ErlPreisBetrieb,
                       Einheit = "%/a", Min = -50, Max = 100,
                       Lesen = p => p.PreissteigerungBetrieb,
                       Setzen = (p, w) => p.PreissteigerungBetrieb = w },

            // p_I: gemeldet wird der WIRKSAME Wert (leer heisst „wie p_B", nicht 0 %/a) -
            // dieselbe Regel wie im Leseweg. Gesetzt wird er danach ausdruecklich.
            new Feld { Name = "preissteigerung_investition_prozent", Anzeigename = KiAktionsTexte.FeldPreisInvest,
                       Erlaeuterung = KiAktionsTexte.ErlPreisInvest,
                       Einheit = "%/a", Min = -50, Max = 100,
                       Lesen = p => p.PreisInvestWirksam,
                       Setzen = (p, w) => p.PreissteigerungInvestition = w },

            new Feld { Name = "einspeiseverguetung_eur_kwh", Anzeigename = KiAktionsTexte.FeldEinspeisung,
                       Erlaeuterung = KiAktionsTexte.ErlEinspeisung,
                       Einheit = "€/kWh", Min = 0, Max = 10,
                       Lesen = p => p.Einspeiseverguetung,
                       Setzen = (p, w) => p.Einspeiseverguetung = w },

            new Feld { Name = "co2_preis_eur_t", Anzeigename = KiAktionsTexte.FeldCo2Preis,
                       Erlaeuterung = KiAktionsTexte.ErlCo2Preis,
                       Einheit = "€/t", Min = 0, Max = 10000,
                       Lesen = p => p.CO2Preis, Setzen = (p, w) => p.CO2Preis = w }
        };

        /// <summary>Projektparameter plus die sieben Felder - alle Felder FREIWILLIG.</summary>
        private static KiParameter[] Parameterfelder()
        {
            var liste = new List<KiParameter> { KiHilfe.ProjektParameter(pflicht: false) };

            foreach (Feld f in FELDER)
                liste.Add(new KiParameter(f.Name, KiParameterTyp.Zahl, f.Erlaeuterung,
                                          pflicht: false, anzeigename: f.Anzeigename,
                                          min: f.Min, max: f.Max, einheit: f.Einheit));

            return liste.ToArray();
        }

        /// <summary>Die Felder, die dieser Aufruf tatsaechlich nennt.</summary>
        private static List<Feld> Genannte(KiAufruf a)
        {
            var liste = new List<Feld>();
            foreach (Feld f in FELDER)
                if (a.Hat(f.Name)) liste.Add(f);
            return liste;
        }

        /// <summary>Ein Zahlwert mit Einheit fuer den Bestaetigungsblock.</summary>
        private static string Zahltext(double wert, string einheit)
        {
            string zahl = wert.ToString("0.####", CultureInfo.CurrentCulture);
            return string.IsNullOrEmpty(einheit) ? zahl : zahl + " " + einheit;
        }
    }
}
