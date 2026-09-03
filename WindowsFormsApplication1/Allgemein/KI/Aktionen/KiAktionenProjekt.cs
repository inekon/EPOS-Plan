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
        // projekt_aktiv
        // =====================================================================

        /// <summary>
        /// GENAU das gerade geoeffnete Projekt. Andockpunkt <c>Dienste.Projekt</c>,
        /// ersatzweise <c>ApplikationCtrl.ReadSingle()</c>; die Kopfdaten liest
        /// <c>ProjektCtrl.ReadSingle(int)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum es diese Aktion gibt.</b> „Wie heisst das aktuelle Projekt?" liess
        /// sich bisher nicht beantworten: Der Kontext, den der Assistent mitbekommt,
        /// nennt nur den BEREICH (<see cref="HilfeKontext.Beschreibung"/>) und streicht
        /// den Projektnamen sogar ausdruecklich heraus. Und aus
        /// <c>projekte_auflisten</c> kann das Modell nicht ablesen, welches der
        /// zwanzig Platzhalterprojekte offen ist. Hier fragt das PROGRAMM die
        /// Oberflaeche - das Modell bekommt eine einzige Zeile.
        /// </para>
        /// <para>
        /// <b>Dieselbe Quelle wie die Kontextsaeuberung.</b> Gelesen wird
        /// <c>Dienste.Projekt</c> - genau die Quelle, aus der
        /// <c>HilfeKontext.OhneKlarnamen</c> ausschneidet. Damit kann die Aktion nie
        /// ein anderes Projekt melden als das, dessen Name aus dem Kontext entfernt
        /// wird. Erst wenn es kein Startfenster gibt (Pruefharnisch, Konsole), gilt
        /// ersatzweise das zuletzt geoeffnete Projekt aus <c>Tab_Applikation</c>.
        /// </para>
        /// <para>
        /// <b>Am Datenschutz aendert sich nichts.</b> Die Zeile laeuft durch dieselbe
        /// Schicht wie jedes Aktionsergebnis: <c>KiRueckmeldung.Erzeuge</c> ersetzt
        /// Projektname, Kunde und Bearbeiter durch Platzhalter, bevor irgendetwas das
        /// Programm verlaesst (Fachkonzept 4.2). An das Modell geht „Name 1", der
        /// Anwender sieht im Chatfenster den Klarnamen.
        /// </para>
        /// <para>
        /// <b>Kein offenes Projekt ist ein ORDENTLICHES Ergebnis</b>, kein Fehler -
        /// beim Programmstart ist das der Regelfall. Der Ergebnissatz sagt es
        /// ausdruecklich, damit das Modell nicht anfaengt zu raten.
        /// </para>
        /// </remarks>
        internal static KiAktion ProjektAktiv()
        {
            return new KiAktion(
                name: "projekt_aktiv",
                zweck: KiAktionsTexte.ZweckProjektAktiv,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "Dienste.Projekt / ApplikationCtrl.ReadSingle + ProjektCtrl.ReadSingle(int)",
                ausfuehren: delegate { return AktivesProjektErgebnis(); });
        }

        /// <summary>
        /// Das Ergebnis der Aktion <c>projekt_aktiv</c>.
        /// </summary>
        /// <remarks>
        /// Eigene Methode und nicht bloss ein Lambda im Register: So erreicht der
        /// Pruefharnisch beide Zweige (Projekt offen / keines offen), ohne die
        /// MDI-Oberflaeche hochzufahren.
        /// </remarks>
        internal static KiErgebnis AktivesProjektErgebnis()
        {
            int id;
            string name;
            if (!AktivesProjektErmitteln(out id, out name))
                return KiErgebnis.Ok(KiAktionsTexte.ProjektAktivKeines);

            // Kopfdaten aus der Datenbank; sie koennen fehlen, wenn das gemerkte
            // Projekt inzwischen geloescht wurde. Dann bleibt der Name der
            // Oberflaeche stehen - er ist das, was der Anwender vor sich sieht.
            var ctrl = new ProjektCtrl();
            if (id > 0) ctrl.ReadSingle(id);

            bool gelesen = id > 0 && ctrl.rows > 0;
            string projektname = gelesen ? (ctrl.m_szProjektname ?? "") : name;
            var meldungen = new List<string>();
            if (!gelesen)
                meldungen.Add(string.Format(CultureInfo.CurrentCulture,
                                            KiAktionsTexte.ProjektAktivNichtGelesen, id));

            var zeilen = KiHilfe.Liste();
            zeilen.Add(KiHilfe.Zeile(
                "id", gelesen ? ctrl.m_ID : id,
                "projektname", KiHilfe.Text(projektname),
                "kunde", KiHilfe.Text(gelesen ? ctrl.m_szKunde : ""),
                "bearbeiter", KiHilfe.Text(gelesen ? ctrl.m_szBearbeiter : ""),
                "geaendert", gelesen ? KiHilfe.Datum(ctrl.m_Aenderungsdatum) : KiHilfe.Text("")));

            // Der Projektname steht ABSICHTLICH auch im Ergebnissatz: KiRueckmeldung
            // baut zuerst die Zeilen (dabei entsteht der Platzhalter) und saeubert den
            // Satz danach - der Klarname wird also auch hier ersetzt. Beide Stellen
            // fuehren denselben Zeichenkettenwert, sonst griffe die Ersetzung nicht.
            KiErgebnis e = KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.ProjektAktivGelesen,
                                                       projektname, gelesen ? ctrl.m_ID : id),
                                         zeilen);
            return meldungen.Count > 0 ? e.MitMeldungen(meldungen) : e;
        }

        /// <summary>
        /// Ermittelt das gerade geoeffnete Projekt. Rueckgabe <c>false</c>, wenn keines
        /// offen ist - dann sind <paramref name="id"/> 0 und <paramref name="name"/> leer.
        /// </summary>
        /// <remarks>
        /// Reihenfolge: erst der laufende Projektkontext (<c>Dienste.Projekt</c>), dann
        /// das zuletzt geoeffnete Projekt aus <c>Tab_Applikation</c>. Fehlt die ID,
        /// wird sie ueber den Namen nachgeschlagen; fehlt der Name, ueber die ID. Jeder
        /// Datenbankzugriff ist eingefangen: Die Aktion darf an einer nicht erreichbaren
        /// Datenbank nicht scheitern, sondern meldet dann „kein Projekt geoeffnet".
        /// </remarks>
        internal static bool AktivesProjektErmitteln(out int id, out string name)
        {
            id = 0;
            name = "";
            bool oberflaecheLaeuft = false;

            try
            {
                // Vorhanden und nicht Id > 0: Laeuft die Oberflaeche, gilt ihr Stand
                // AUCH dann, wenn gerade kein Projekt offen ist - die Antwort lautet
                // dann "keins" und nicht "das zuletzt geoeffnete".
                if (Dienste.Projekt.Vorhanden)
                {
                    oberflaecheLaeuft = true;
                    id = Dienste.Projekt.Id;
                    name = Dienste.Projekt.Name ?? "";
                }
            }
            catch { oberflaecheLaeuft = false; id = 0; name = ""; }

            if (!oberflaecheLaeuft)
            {
                // NUR ohne Startfenster (Pruefharnisch, Konsole): das zuletzt
                // geoeffnete Projekt aus Tab_Applikation - dieselbe Quelle, aus der
                // Form_Start.pBox_ProjektZuletzt_Click schoepft.
                //
                // Laeuft die Oberflaeche und hat sie kein Projekt geladen, ist „keines
                // offen" die RICHTIGE Antwort. „Zuletzt geoeffnet" waere eine andere
                // Frage - und der Assistent wuerde ein Projekt als aktiv ausgeben, das
                // der Anwender gar nicht vor sich hat.
                try
                {
                    var app = new ApplikationCtrl();
                    app.ReadSingle();
                    if (app.rows > 0)
                    {
                        id = app.m_ID_Projekt;
                        name = app.m_szProjektname ?? "";
                    }
                }
                catch { }
            }

            name = (name ?? "").Trim();
            if (id <= 0 && name.Length == 0) return false;

            // Die fehlende Haelfte nachschlagen. Tab_Applikation fuehrte die ID
            // historisch nicht immer mit (Befund 3 in Form_Start.cs) - der Name ist
            // dort der verlaessliche Teil.
            try
            {
                if (id <= 0 && name.Length > 0)
                {
                    var ctrl = new ProjektCtrl();
                    ctrl.ReadSingle(name);
                    if (ctrl.rows > 0) id = ctrl.m_ID;
                }
                else if (id > 0 && name.Length == 0)
                {
                    name = KiHilfe.ProjektName(id);
                }
            }
            catch { }

            return id > 0 || name.Length > 0;
        }

        // =====================================================================
        // projekt_suchen
        // =====================================================================

        /// <summary>
        /// Projekte, deren Name oder Kunde den Suchtext enthaelt. Andockpunkt
        /// <c>ProjektCtrl.ReadAll()</c> ueber <see cref="KiHilfe.ProjektKandidaten"/>;
        /// gefiltert wird LOKAL.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum es diese Aktion gibt.</b> Die Datenschutzschicht ersetzt jeden
        /// Bezeichner der Ergebniszeilen durch einen Platzhalter (Fachkonzept 4.2). Das
        /// Modell kann einen Namen aus der Anwenderfrage in einer Ergebnisliste deshalb
        /// NIE wiederfinden - es zog daraus im Betrieb den falschen Schluss, das Projekt
        /// gebe es nicht (Fehlerfall 23.08.2026). Hier vergleicht das PROGRAMM, nicht das
        /// Modell: Es sieht die Klarnamen und kann die Frage beantworten, die das Modell
        /// selbst nicht beantworten kann.
        /// </para>
        /// <para>
        /// <b>Dieselbe Quelle wie die Namensaufloesung.</b> Gefiltert wird
        /// <see cref="KiHilfe.ProjektKandidaten"/> - genau die Liste, aus der auch
        /// <see cref="KiHilfe.Waehle"/> schoepft. Damit gilt: Was diese Aktion als Treffer
        /// meldet, nimmt der Parameter „Projekt" hinterher auch an.
        /// </para>
        /// <para>
        /// <b>Kein Treffer ist ein ORDENTLICHES Ergebnis</b>, kein Fehler. Der Abgleich
        /// lief lokal ueber die vollstaendige Liste; das Ergebnis ist damit vollstaendig
        /// und belastbar - anders als der Blick des Modells auf zwanzig Platzhalterzeilen.
        /// Der Ergebnissatz sagt das ausdruecklich, damit das Modell nicht noch einmal
        /// aus einer Anzeigebeschraenkung eine Tatsache macht.
        /// </para>
        /// </remarks>
        internal static KiAktion ProjektSuchen()
        {
            return new KiAktion(
                name: "projekt_suchen",
                zweck: KiAktionsTexte.ZweckProjektSuchen,
                stufe: Schutzstufe.Lesen,
                andockpunkt: "ProjektCtrl.ReadAll (lokaler Teiltreffer)",
                parameter: new[]
                {
                    new KiParameter("suchtext", KiParameterTyp.Text,
                                    KiAktionsTexte.ErlSuchtext,
                                    anzeigename: KiAktionsTexte.SuchtextName, maxLaenge: 200)
                },
                ausfuehren: a =>
                {
                    string gesucht = (a.Text("suchtext") ?? "").Trim();
                    List<KiHilfe.Kandidat> alle = KiHilfe.ProjektKandidaten();

                    var zeilen = KiHilfe.Liste();
                    foreach (KiHilfe.Kandidat k in alle)
                    {
                        // CurrentCultureIgnoreCase wie in KiHilfe.Waehle - beide Stellen
                        // muessen dieselbe Vorstellung von „passt" haben, sonst bietet die
                        // Suche etwas an, das die Aufloesung danach ablehnt.
                        bool trifft =
                            k.Name.IndexOf(gesucht, StringComparison.CurrentCultureIgnoreCase) >= 0
                            || k.Zusatz.IndexOf(gesucht, StringComparison.CurrentCultureIgnoreCase) >= 0;
                        if (!trifft) continue;

                        zeilen.Add(KiHilfe.Zeile(
                            "id", k.Id,
                            "projektname", KiHilfe.Text(k.Name),
                            "kunde", KiHilfe.Text(k.Zusatz)));
                    }

                    if (zeilen.Count == 0)
                        return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                           KiAktionsTexte.ProjektSucheKeine,
                                                           gesucht, alle.Count));

                    return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                       KiAktionsTexte.ProjektSucheGefunden,
                                                       zeilen.Count, alle.Count, gesucht),
                                         zeilen);
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
