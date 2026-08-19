using System;
using System.Collections.Generic;
using System.Data.OleDb;
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
                stufe: Schutzstufe.Lesen,
                andockpunkt: "WirtschaftlichkeitCtrl.LadeErgebnisse / ErgebnisAktuell",
                parameter: new[]
                {
                    new KiParameter("projekt_ids", KiParameterTyp.GanzzahlListe,
                                    KiAktionsTexte.ErlProjekteFuerErgebnisse,
                                    anzeigename: KiAktionsTexte.ProjekteName, min: 1)
                },
                ausfuehren: a =>
                {
                    var ids = new List<int>(a.IdListe("projekt_ids"));
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
                stufe: Schutzstufe.Lesen,
                andockpunkt: "WirtschaftlichkeitCtrl.LadeParameter / LadeTarif",
                parameter: new[] { KiHilfe.ProjektId() },
                vorbedingung: a => KiHilfe.ProjektMussExistieren(a.Id("projekt_id")),
                ausfuehren: a =>
                {
                    int id = a.Id("projekt_id");
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
                stufe: Schutzstufe.Lesen,
                andockpunkt: "KostenPositionCtrl.Pruefe / TechnikPlanwertCtrl.LiesAnlagen",
                parameter: new[]
                {
                    KiHilfe.ProjektId(),
                    new KiParameter("komponente", KiParameterTyp.Aufzaehlung,
                                    KiAktionsTexte.ErlKomponente,
                                    anzeigename: KiAktionsTexte.KomponenteName,
                                    werte: Komponenten)
                },
                vorbedingung: a =>
                {
                    int id = a.Id("projekt_id");
                    string komponente = a.Text("komponente");

                    string grund = KiHilfe.ProjektMussExistieren(id);
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
                    int id = a.Id("projekt_id");
                    string komponente = a.Text("komponente");

                    int komponentenId = KomponentenId(komponente);
                    if (komponentenId <= 0)
                        return KiErgebnis.Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                                                  KiAktionsTexte.KostenlageOhneKomponente,
                                                                  komponente));

                    KostenPositionCtrl.Abweichung ab = KostenPositionCtrl.Pruefe(
                        id, komponente, Form_Kosten.KATEGORIE_INVESTITION, komponentenId);

                    List<TechnikPlanwertCtrl.Anlage> anlagen = TechnikPlanwertCtrl.LiesAnlagen(id, komponente);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_projekt", id,
                        "komponente", komponente,
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
                    new OleDbParameter("@k", komponente ?? ""));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }
    }
}
