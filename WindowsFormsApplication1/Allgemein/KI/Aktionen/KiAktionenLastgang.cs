using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KiKern;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Leseaktionen rund um Lastgang und Peak-Shaving (Fachkonzept 5.1, Zeilen 10-12).
    /// </summary>
    internal static class KiAktionenLastgang
    {
        // =====================================================================
        // lastgang_pruefen
        // =====================================================================

        /// <summary>
        /// Prueft eine Lastgangdatei. Andockpunkt <c>GanglinienDatei.Erkenne(string)</c>
        /// und <c>Vorschau(string, GanglinienImportOptionen)</c>.
        /// </summary>
        /// <remarks>
        /// Die Kette meldet Befunde als <see cref="PruefMeldung"/> im Protokoll und wirft
        /// nicht (<c>GanglinienDatei.cs:360</c>) - die Meldungen gehen deshalb als
        /// sprachneutrale Kurzfassung in das Ergebnis. IMPORTIERT WIRD NICHTS.
        /// </remarks>
        internal static KiAktion LastgangPruefen()
        {
            return new KiAktion(
                name: "lastgang_pruefen",
                zweck: KiAktionsTexte.ZweckLastgangPruefen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "GanglinienDatei.Erkenne / GanglinienDatei.Vorschau",
                parameter: new[]
                {
                    new KiParameter("dateipfad", KiParameterTyp.Text, KiAktionsTexte.ErlDateipfad,
                                    anzeigename: KiAktionsTexte.DateipfadName, maxLaenge: 260)
                },
                vorbedingung: a =>
                {
                    string pfad = a.Text("dateipfad");
                    try
                    {
                        if (File.Exists(pfad)) return null;
                    }
                    catch (ArgumentException) { }
                    catch (NotSupportedException) { }
                    catch (PathTooLongException) { }
                    return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.DateiFehlt, pfad);
                },
                ausfuehren: a =>
                {
                    string pfad = a.Text("dateipfad");

                    GanglinienVorschau v = GanglinienDatei.Erkenne(pfad);
                    if (v.Lesbar)
                    {
                        // Zweiter Schritt mit dem erkannten Vorschlag - erst er fuellt die
                        // Zeilenvorschau und meldet unlesbare Zahlen.
                        GanglinienVorschau fein = GanglinienDatei.Vorschau(pfad, v.Vorschlag);
                        if (fein != null) v = fein;
                    }

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "dateiname", Path.GetFileName(pfad),
                        "lesbar", v.Lesbar,
                        "ist_excel", v.IstExcel,
                        "spaltenzahl", v.Spaltenzahl,
                        "blaetter", v.Blaetter.Count,
                        "wertspalte", v.Vorschlag.WertSpalte,
                        "zeitspalte", v.Vorschlag.ZeitSpalte,
                        "kopfzeile", v.Vorschlag.Kopfzeile,
                        "trennzeichen", GanglinienDatei.TrennzeichenText(v.Vorschlag.Trennzeichen),
                        "dezimaltrenner", v.Vorschlag.Dezimaltrenner.ToString(),
                        "raster", v.Vorschlag.Raster.ToString(),
                        "einheit", v.Vorschlag.Einheit.ToString(),
                        "vorschauzeilen", v.Zeilen.Count));

                    var meldungen = new List<string>();
                    foreach (PruefMeldung m in v.Meldungen) meldungen.Add(m.ToString());

                    string text = v.Lesbar
                        ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.LastgangLesbar,
                                        v.Spaltenzahl, v.Vorschlag.WertSpalte, v.Vorschlag.Raster)
                        : KiAktionsTexte.LastgangNichtLesbar;

                    return KiErgebnis.Ok(text, zeilen, anzahl: 1).MitMeldungen(meldungen);
                });
        }

        // =====================================================================
        // ganglinien_auflisten
        // =====================================================================

        /// <summary>
        /// Waehlbare Ganglinien. Andockpunkt <c>PeakShavingCtrl.LeseGanglinien(int)</c>.
        /// </summary>
        internal static KiAktion GanglinienAuflisten()
        {
            return new KiAktion(
                name: "ganglinien_auflisten",
                zweck: KiAktionsTexte.ZweckGanglinienAuflisten,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "PeakShavingCtrl.LeseGanglinien",
                parameter: new[]
                {
                    // 0 bzw. weggelassen = nur der Stammkatalog; die Maske ist ausdruecklich
                    // auch ohne geoeffnetes Projekt nutzbar (PeakShavingCtrl.cs:142).
                    new KiParameter("projekt_id", KiParameterTyp.Ganzzahl,
                                    KiAktionsTexte.ErlProjektIdGanglinien,
                                    pflicht: false, anzeigename: KiAktionsTexte.ProjektIdName, min: 0)
                },
                ausfuehren: a =>
                {
                    int id = a.Id("projekt_id");
                    List<GanglinienEintrag> liste = PeakShavingCtrl.LeseGanglinien(id);

                    var zeilen = KiHilfe.Liste();
                    int ausProjekt = 0;
                    foreach (GanglinienEintrag g in liste)
                    {
                        if (!g.AusStamm) ausProjekt++;
                        zeilen.Add(KiHilfe.Zeile(
                            "id", g.Id,
                            "bezeichner", KiHilfe.Text(g.Bezeichner),
                            "zeitinterval", g.Zeitinterval,
                            "aus_stamm", g.AusStamm));
                    }

                    if (zeilen.Count == 0) return KiErgebnis.Ok(KiAktionsTexte.GanglinienKeine);

                    return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.GanglinienGefunden,
                                                       zeilen.Count, ausProjekt, zeilen.Count - ausProjekt),
                                         zeilen);
                });
        }

        // =====================================================================
        // minimale_spitze_ermitteln
        // =====================================================================

        /// <summary>
        /// Kleinste haltbare Netzbezugsspitze. Andockpunkt
        /// <c>SpeicherEngine.PeakShaving.MinimaleSchwelleKw(double[], SpeicherParameter,
        /// SpeicherModus, int)</c>; der Lastgang kommt aus
        /// <c>PeakShavingCtrl.LeseWerte(GanglinienEintrag)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Der Rechenteil ist rein (keine Datenbank) und duerfte nach Fachkonzept 3.4 in
        /// <c>Task.Run</c> laufen. In diesem Paket laeuft er wie jede andere Aktion auf dem
        /// UI-Thread: die Bisektion braucht 60 Durchlaeufe ueber 35.040 Werte und ist damit
        /// im Bereich weniger hundert Millisekunden. Die Auslagerung gehoert zu den
        /// Rechenaktionen der Etappe 4, die den Fortschrittsweg ohnehin mitbringen.
        /// </para>
        /// <para>
        /// ZUSATZPARAMETER gegenueber Fachkonzept 5.1: <c>projekt_id</c>. Ohne ihn waeren
        /// nur Stammganglinien auffindbar - <c>LeseGanglinien</c> liefert Projektganglinien
        /// nur bei gesetztem Projekt.
        /// </para>
        /// </remarks>
        internal static KiAktion MinimaleSpitzeErmitteln()
        {
            return new KiAktion(
                name: "minimale_spitze_ermitteln",
                zweck: KiAktionsTexte.ZweckMinimaleSpitze,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "PeakShaving.MinimaleSchwelleKw",
                parameter: new[]
                {
                    new KiParameter("ganglinie_id", KiParameterTyp.Ganzzahl, KiAktionsTexte.ErlGanglinieId,
                                    anzeigename: KiAktionsTexte.GanglinieName, min: 1),
                    new KiParameter("kapazitaet_kwh", KiParameterTyp.Zahl, KiAktionsTexte.ErlKapazitaet,
                                    anzeigename: KiAktionsTexte.KapazitaetName,
                                    min: 0.001, max: 10000000, einheit: "kWh"),
                    new KiParameter("leistung_kw", KiParameterTyp.Zahl, KiAktionsTexte.ErlLeistung,
                                    anzeigename: KiAktionsTexte.LeistungName,
                                    min: 0.001, max: 10000000, einheit: "kW"),
                    new KiParameter("wirkungsgrad_rt", KiParameterTyp.Zahl, KiAktionsTexte.ErlWirkungsgrad,
                                    pflicht: false, anzeigename: KiAktionsTexte.WirkungsgradName,
                                    min: 0.01, max: 1.0),
                    new KiParameter("soc_min_prozent", KiParameterTyp.Zahl, KiAktionsTexte.ErlSocMin,
                                    pflicht: false, anzeigename: KiAktionsTexte.SocMinName,
                                    min: 0, max: 100, einheit: "%"),
                    new KiParameter("soc_max_prozent", KiParameterTyp.Zahl, KiAktionsTexte.ErlSocMax,
                                    pflicht: false, anzeigename: KiAktionsTexte.SocMaxName,
                                    min: 0, max: 100, einheit: "%"),
                    new KiParameter("projekt_id", KiParameterTyp.Ganzzahl,
                                    KiAktionsTexte.ErlProjektIdGanglinieSuche,
                                    pflicht: false, anzeigename: KiAktionsTexte.ProjektIdName, min: 0)
                },
                vorbedingung: a =>
                {
                    double socMin = a.Zahl("soc_min_prozent", StromspeicherVarianteModel.SOC_MIN_VORGABE);
                    double socMax = a.Zahl("soc_max_prozent", StromspeicherVarianteModel.SOC_MAX_VORGABE);
                    if (socMin >= socMax) return KiAktionsTexte.SocVerdreht;

                    if (Ganglinie(a.Id("ganglinie_id"), a.Id("projekt_id")) == null)
                        return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.GanglinieUnbekannt,
                                             a.Id("ganglinie_id"));
                    return null;
                },
                ausfuehren: a =>
                {
                    int idGanglinie = a.Id("ganglinie_id");
                    GanglinienEintrag eintrag = Ganglinie(idGanglinie, a.Id("projekt_id"));

                    double[] last = PeakShavingCtrl.LeseWerte(eintrag);
                    if (last == null || last.Length == 0)
                        return KiErgebnis.Abgelehnt(string.Format(CultureInfo.CurrentCulture,
                                                                  KiAktionsTexte.GanglinieLeer, idGanglinie));

                    double kapazitaet = a.Zahl("kapazitaet_kwh");
                    double socMin = a.Zahl("soc_min_prozent", StromspeicherVarianteModel.SOC_MIN_VORGABE);
                    double socMax = a.Zahl("soc_max_prozent", StromspeicherVarianteModel.SOC_MAX_VORGABE);

                    var p = new SpeicherParameter
                    {
                        CNomKwh = kapazitaet,
                        PKw = a.Zahl("leistung_kw"),
                        SoCMinKwh = kapazitaet * socMin / 100.0,
                        SoCMaxKwh = kapazitaet * socMax / 100.0,
                        RoundTripWirkungsgrad = a.Zahl("wirkungsgrad_rt",
                                                       StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE),
                        // Der Lastgang liegt nach LeseWerte im Viertelstundenraster.
                        DtH = 0.25
                    };

                    double spitze = PeakShaving.MinimaleSchwelleKw(last, p);

                    double ausgang = 0.0;
                    for (int i = 0; i < last.Length; i++) if (last[i] > ausgang) ausgang = last[i];

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "ganglinie_id", idGanglinie,
                        "bezeichner", KiHilfe.Text(eintrag.Bezeichner),
                        "werte", last.Length,
                        "spitze_vorher_kw", KiHilfe.Wert(ausgang),
                        "spitze_minimal_kw", KiHilfe.Wert(spitze),
                        "ersparnis_kw", KiHilfe.Wert(ausgang - spitze),
                        "kapazitaet_kwh", KiHilfe.Wert(p.CNomKwh),
                        "leistung_kw", KiHilfe.Wert(p.PKw),
                        "wirkungsgrad_rt", KiHilfe.Wert(p.RoundTripWirkungsgrad),
                        "soc_min_prozent", KiHilfe.Wert(socMin),
                        "soc_max_prozent", KiHilfe.Wert(socMax)));

                    return KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SpitzeErmittelt,
                                      spitze.ToString("N1", CultureInfo.CurrentCulture),
                                      ausgang.ToString("N1", CultureInfo.CurrentCulture),
                                      (ausgang - spitze).ToString("N1", CultureInfo.CurrentCulture)),
                        zeilen, anzahl: 1);
                });
        }

        /// <summary>Sucht eine Ganglinie in Projekt und Stammkatalog; <c>null</c>, wenn unbekannt.</summary>
        private static GanglinienEintrag Ganglinie(int idGanglinie, int idProjekt)
        {
            if (idGanglinie <= 0) return null;
            try
            {
                foreach (GanglinienEintrag g in PeakShavingCtrl.LeseGanglinien(idProjekt))
                    if (g.Id == idGanglinie) return g;
            }
            catch { }
            return null;
        }
    }
}
