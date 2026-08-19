using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Leseaktionen rund um Projekte, Varianten und Speichervarianten
    /// (Fachkonzept 5.1, Zeilen 1-4).
    /// </summary>
    internal static class KiAktionenProjekt
    {
        // =====================================================================
        // projekte_auflisten
        // =====================================================================

        /// <summary>Alle Projekte. Andockpunkt <c>ProjektCtrl.ReadAll()</c>.</summary>
        internal static KiAktion ProjekteAuflisten()
        {
            return new KiAktion(
                name: "projekte_auflisten",
                zweck: KiAktionsTexte.ZweckProjekteAuflisten,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "ProjektCtrl.ReadAll",
                ausfuehren: delegate
                {
                    var ctrl = new ProjektCtrl();
                    ctrl.ReadAll();

                    var zeilen = KiHilfe.Liste();
                    foreach (ProjektModel p in ctrl.items)
                    {
                        zeilen.Add(KiHilfe.Zeile(
                            "id", p.m_ID,
                            "projektname", KiHilfe.Text(p.m_szProjektname),
                            "kunde", KiHilfe.Text(p.m_szKunde),
                            "bearbeiter", KiHilfe.Text(p.m_szBearbeiter),
                            "geaendert", KiHilfe.Datum(p.m_Aenderungsdatum)));
                    }

                    string text = zeilen.Count == 0
                        ? KiAktionsTexte.ProjekteKeine
                        : string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.ProjekteGefunden, zeilen.Count);
                    return KiErgebnis.Ok(text, zeilen);
                });
        }

        // =====================================================================
        // projekt_lesen
        // =====================================================================

        /// <summary>Kopfdaten eines Projekts. Andockpunkt <c>ProjektCtrl.ReadSingle(int)</c>.</summary>
        internal static KiAktion ProjektLesen()
        {
            return new KiAktion(
                name: "projekt_lesen",
                zweck: KiAktionsTexte.ZweckProjektLesen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "ProjektCtrl.ReadSingle(int)",
                parameter: new[] { KiHilfe.ProjektParameter() },
                vorbedingung: a => KiHilfe.ProjektMussAufloesbarSein(a),
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    var ctrl = new ProjektCtrl();
                    ctrl.ReadSingle(id);

                    // rows == 0 kann trotz bestandener Vorbedingung auftreten, wenn zwischen
                    // Pruefung und Lauf jemand das Projekt geloescht hat.
                    if (ctrl.rows == 0)
                        return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                           KiAktionsTexte.ProjektUnbekannt, id));

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id", ctrl.m_ID,
                        "projektname", KiHilfe.Text(ctrl.m_szProjektname),
                        "kunde", KiHilfe.Text(ctrl.m_szKunde),
                        "bearbeiter", KiHilfe.Text(ctrl.m_szBearbeiter),
                        "beschreibung", KiHilfe.Text(ctrl.m_szBeschreibung),
                        "id_klimaregion", ctrl.m_ID_Klimaregion,
                        "erstellt", KiHilfe.Datum(ctrl.m_Erstelldatum),
                        "geaendert", KiHilfe.Datum(ctrl.m_Aenderungsdatum)));

                    return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.ProjektGelesen,
                                                       ctrl.m_ID, ctrl.m_szProjektname),
                                         zeilen);
                });
        }

        // =====================================================================
        // varianten_auflisten
        // =====================================================================

        /// <summary>
        /// Stamm und Varianten einer Vergleichsgruppe. Andockpunkt
        /// <c>VariantenCtrl.LadeGruppe(int, string)</c>; ist die uebergebene ID selbst eine
        /// Variante, loest <c>StammRefDerVariante</c> zuerst auf - und der Assistent sagt es.
        /// </summary>
        internal static KiAktion VariantenAuflisten()
        {
            return new KiAktion(
                name: "varianten_auflisten",
                zweck: KiAktionsTexte.ZweckVariantenAuflisten,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "VariantenCtrl.LadeGruppe / StammRefDerVariante",
                parameter: new[] { KiHilfe.ProjektParameter() },
                vorbedingung: a => KiHilfe.ProjektMussAufloesbarSein(a),
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    var ctrl = new VariantenCtrl();

                    string hinweis = null;
                    int idStamm = id;
                    int stammRef = ctrl.StammRefDerVariante(id);
                    if (stammRef > 0)
                    {
                        idStamm = stammRef;
                        hinweis = string.Format(CultureInfo.CurrentCulture,
                                                KiAktionsTexte.VarianteAufgeloest, id, idStamm);
                    }

                    string stammName = KiHilfe.ProjektName(idStamm);
                    List<VariantenCtrl.VarianteInfo> gruppe = ctrl.LadeGruppe(idStamm, stammName);

                    var zeilen = KiHilfe.Liste();
                    foreach (VariantenCtrl.VarianteInfo v in gruppe)
                    {
                        zeilen.Add(KiHilfe.Zeile(
                            "id", v.IdProjekt,
                            "projektname", KiHilfe.Text(v.Projektname),
                            "variantenname", KiHilfe.Text(v.Variantenname),
                            "ist_stamm", v.IstStamm));
                    }

                    int varianten = zeilen.Count - 1;
                    string text = varianten <= 0
                        ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.EinzelnesProjekt, stammName)
                        : string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.VariantenGruppe, stammName,
                                        KiHilfe.Anzahltext(varianten, "Variante", "Varianten"));

                    KiErgebnis e = KiErgebnis.Ok(text, zeilen);
                    if (hinweis != null) e.MitMeldungen(new[] { hinweis });
                    return e;
                });
        }

        // =====================================================================
        // speichervarianten_auflisten
        // =====================================================================

        /// <summary>
        /// Speichervarianten eines Projekts. Andockpunkt
        /// <c>StromspeicherVarianteCtrl.ReadAllByProjekt(int)</c>, aktive Variante ueber
        /// <c>ReadAktiveVariante(int)</c>.
        /// </summary>
        /// <remarks>
        /// BEWUSSTE ABWEICHUNG vom Fachkonzept 5.1: Dort lautet die Vorbedingung
        /// „Stromspeichermodul freigeschaltet". Eine solche Modulfreischaltung gibt es im
        /// Bestand nicht - <c>LizenzManager</c> kennt nur <c>DarfSchreiben()</c> und keine
        /// Modulliste. Geprueft wird deshalb, was tatsaechlich fehlschlagen kann: eine
        /// Datenbank ohne die Tabellen des Moduls. Der Aufruf faengt den
        /// <see cref="OleDbException"/> ab und meldet ihn im Klartext.
        /// </remarks>
        internal static KiAktion SpeichervariantenAuflisten()
        {
            return new KiAktion(
                name: "speichervarianten_auflisten",
                zweck: KiAktionsTexte.ZweckSpeichervariantenAuflisten,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "StromspeicherVarianteCtrl.ReadAllByProjekt / ReadAktiveVariante",
                parameter: new[] { KiHilfe.ProjektParameter() },
                vorbedingung: a => KiHilfe.ProjektMussAufloesbarSein(a),
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    var ctrl = new StromspeicherVarianteCtrl();

                    List<StromspeicherVarianteModel> varianten;
                    StromspeicherVarianteModel aktive;
                    try
                    {
                        varianten = ctrl.ReadAllByProjekt(id);
                        aktive = new StromspeicherVarianteCtrl().ReadAktiveVariante(id);
                    }
                    catch (OleDbException ex)
                    {
                        return KiErgebnis.Abgelehnt(KiAktionsTexte.SpeicherTabelleFehlt)
                                         .MitMeldungen(new[] { ex.Message });
                    }

                    var zeilen = KiHilfe.Liste();
                    foreach (StromspeicherVarianteModel v in varianten)
                    {
                        zeilen.Add(KiHilfe.Zeile(
                            "id", v.ID,
                            "id_energieanlage", v.ID_Energieanlage,
                            "betriebsart", KiHilfe.Text(v.Betriebsart),
                            "berechnungsart", KiHilfe.Text(v.Berechnungsart),
                            "preisquelle", KiHilfe.Text(v.Preisquelle),
                            "soc_min_prozent", KiHilfe.Wert(v.SoC_Min_Prozent),
                            "soc_max_prozent", KiHilfe.Wert(v.SoC_Max_Prozent),
                            "kapitalzins", KiHilfe.Wert(v.Kapitalzins),
                            "nutzungsdauer_a", KiHilfe.Wert(v.Nutzungsdauer),
                            "leistungspreis", KiHilfe.Wert(v.L_P),
                            "aktiv", v.Aktiv));
                    }

                    if (zeilen.Count == 0)
                        return KiErgebnis.Ok(KiAktionsTexte.SpeichervariantenKeine);

                    string aktivText = aktive != null
                        ? aktive.ID.ToString(CultureInfo.CurrentCulture)
                        : KiAktionsTexte.SpeichervarianteKeineAktive;

                    return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.SpeichervariantenGefunden,
                                                       zeilen.Count, aktivText),
                                         zeilen);
                });
        }
    }
}
