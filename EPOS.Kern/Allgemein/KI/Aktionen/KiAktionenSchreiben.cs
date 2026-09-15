using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Schreibaktionen der Stufe 2 (Fachkonzept 5.2, Etappe 3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Fuenf hier, drei anderswo - der Katalog 5.2 ist seit dem 15.09.2026
    /// vollstaendig.</b> Bis dahin standen hier nur die drei, die sich sofort
    /// vollstaendig absichern liessen; die uebrigen haengen an mehrstufigen
    /// Trockenlaeufen oder an einer Mehrdeutigkeit und brauchten die eigene Runde, die
    /// sie jetzt bekommen haben. Zwei davon liegen NICHT hier, sondern bei ihren
    /// Verwandten: <c>komponente_uebernehmen</c> und <c>merkmal_uebernehmen</c> in
    /// <see cref="KiAktionenUebernahme"/> (dort IST der Trockenlauf ihre Vorschau) und
    /// <c>wirtschaftlichkeit_parameter_setzen</c> in <see cref="KiAktionenWirtschaft"/>
    /// (dort steht der Leseweg, aus dem seine Vorschau den Vorzustand nimmt). Eine
    /// Aktion gehoert zu ihrer Fachlichkeit, nicht zu ihrer Schutzstufe.
    /// </para>
    /// <para>
    /// <b>Keine eigene Fachlogik.</b> Jede Aktion ruft genau die Bestandsmethode, die es
    /// schon gibt; die Transaktionsklammer bringt der Bestand mit, wo er eine hat
    /// (Fachkonzept 4.4, Punkt 2). Neu ist hier nur, was die Bestaetigungsschicht
    /// braucht: die Vorbedingung, die Vorschau und die Rueckmeldung.
    /// </para>
    /// <para>
    /// <b>Jede Aktion prueft ihre Zieltabelle auf Schreibschutz</b>
    /// (<see cref="KiSchreibschutz"/>, Fachkonzept 4.5). Die drei Zieltabellen sind
    /// Projekttabellen und fuehren im heutigen Schema kein Feld <c>ReadOnly</c>; die
    /// Wache ist schematolerant und greift, sobald eine Migration es nachtraegt. Ein
    /// Katalogsatz (<c>*_STAMM</c>) wird pauschal abgewiesen.
    /// </para>
    /// <para>
    /// <b>Erkennbarkeit.</b> Jede geschriebene Aenderung setzt das Aenderungsdatum des
    /// betroffenen Projekts (<c>ProjektCtrl.m_Aenderungsdatum</c>) - zusammen mit der
    /// Protokollzeile ist damit im Nachhinein zuzuordnen, was der Assistent getan hat.
    /// </para>
    /// </remarks>
    internal static class KiAktionenSchreiben
    {
        // =====================================================================
        // variante_anlegen
        // =====================================================================

        /// <summary>
        /// Legt aus einem Stammprojekt eine Variante an. Andockpunkt
        /// <c>VariantenCtrl.AnlegenAusStamm(int, string, string, int, out string)</c>,
        /// der intern <c>ProjektDuplizierenCtrl.Duplizieren</c> nutzt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// NICHT umkehrbar: <c>VariantenCtrl.LoescheVariante</c> steht ausdruecklich nicht
        /// im Register (Fachkonzept 5.4). Das sagt die Bestaetigung so.
        /// </para>
        /// <para>
        /// <b>Zwei Projekte, zwei Rollen</b> (Auftrag <b>#240</b>, nach #237). Der
        /// PFLICHTparameter <c>stammprojekt</c> gibt Namen und Gruppenzugehoerigkeit,
        /// der OPTIONALE <c>quellprojekt</c> den INHALT. Ohne ihn bleibt alles wie
        /// bisher - er ist genau der Sonderfall „Quelle = Stamm", den die
        /// Bestandsmethode selbst faehrt. Der Weg gibt es damit dreimal mit EINER
        /// Regel: Kopfband, Menuepunkt „Als Variante speichern…" und hier.
        /// </para>
        /// <para>
        /// <b>Die Namensregel steht im Kern</b> (<c>VariantenCtrl.Zielname</c>). Bis
        /// #240 stand hier ein Spiegel davon, damit die Vorschau den Namen nennen
        /// konnte, der hinterher wirklich dasteht; seit #237 gibt es die Regel als
        /// oeffentliche Methode, und ein Spiegel waere seither nur noch eine zweite
        /// Fassung, die auseinanderlaufen kann.
        /// </para>
        /// </remarks>
        internal static KiAktion VarianteAnlegen()
        {
            return new KiAktion(
                name: "variante_anlegen",
                zweck: KiAktionsTexte.ZweckVarianteAnlegen,
                titel: KiAktionsTexte.TitelVarianteAnlegen,
                beispiel: KiAktionsTexte.BeispielVarianteAnlegen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "VariantenCtrl.AnlegenAusStamm",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlStammId, name: "stammprojekt",
                                             anzeigename: KiAktionsTexte.StammIdName),
                    new KiParameter("bezeichner", KiParameterTyp.Text,
                                    KiAktionsTexte.ErlBezeichner,
                                    anzeigename: KiAktionsTexte.BezeichnerName, maxLaenge: 60),
                    // OPTIONAL (Auftrag #240): woher der INHALT kommt. Ohne Angabe wie
                    // bisher aus dem Stamm - derselbe Sonderfall, den auch
                    // VariantenCtrl.AnlegenAusStamm mit idQuelle = idStamm faehrt.
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlQuellprojekt, pflicht: false,
                                             name: "quellprojekt",
                                             anzeigename: KiAktionsTexte.QuellprojektName)
                },
                vorbedingung: a => VarianteVorbedingung(a),
                vorschau: a =>
                {
                    int idStamm = KiHilfe.ProjektId(a, "stammprojekt");
                    string stammName = KiHilfe.ProjektName(idStamm);
                    string bezeichner = a.Text("bezeichner").Trim();

                    // Die Anlagenzahl gehoert zum INHALT, und der kommt seit #240
                    // wahlweise aus einem anderen Projekt.
                    int idQuelle = Quellprojekt(a, idStamm);
                    int idInhalt = idQuelle > 0 ? idQuelle : idStamm;
                    int anlagen = Skalar("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", idInhalt);

                    // DIE Namensregel steht im Kern (VariantenCtrl.Zielname) und wird
                    // hier gerufen, nicht nachgebaut (Auftrag #240): Die Vorschau muss
                    // den Namen nennen, der hinterher wirklich dasteht.
                    string ziel = new VariantenCtrl().Zielname(stammName, bezeichner);

                    string satz = string.Format(CultureInfo.CurrentCulture,
                                                KiAktionsTexte.VorschauVarianteAnlegen,
                                                stammName, idStamm, bezeichner, ziel, anlagen);

                    if (idQuelle > 0)
                        satz += " " + string.Format(CultureInfo.CurrentCulture,
                                                    KiAktionsTexte.VorschauVarianteQuelle,
                                                    KiHilfe.ProjektName(idQuelle), idQuelle);
                    return satz;
                },
                wirkung: KiAktionsTexte.WirkungVarianteAnlegen,
                umkehrbar: false,
                ausfuehren: a =>
                {
                    int idStamm = KiHilfe.ProjektId(a, "stammprojekt");
                    string stammName = KiHilfe.ProjektName(idStamm);
                    string bezeichner = a.Text("bezeichner").Trim();
                    int idQuelle = Quellprojekt(a, idStamm);

                    var ctrl = new VariantenCtrl();
                    string fehler;
                    int neueId = ctrl.AnlegenAusStamm(idStamm, stammName, bezeichner,
                                                      idQuelle > 0 ? idQuelle : idStamm, out fehler);

                    if (neueId <= 0)
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.VarianteFehlgeschlagen,
                                          fehler ?? ""));

                    string neuerName = KiHilfe.ProjektName(neueId);
                    string datumsmeldung = AenderungsdatumSetzen(neueId);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_stamm", idStamm,
                        "id_variante", neueId,
                        "projektname", KiHilfe.Text(neuerName),
                        "variantenname", KiHilfe.Text(bezeichner)));

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.VarianteAngelegt,
                                      bezeichner, neueId, neuerName),
                        zeilen, anzahl: 1);

                    if (datumsmeldung != null) e.MitMeldungen(new[] { datumsmeldung });
                    return e;
                });
        }

        private static string VarianteVorbedingung(KiAufruf a)
        {
            string grund = KiHilfe.ProjektMussAufloesbarSein(a, "stammprojekt");
            if (grund != null) return grund;

            int idStamm = KiHilfe.ProjektId(a, "stammprojekt");

            string bezeichner = a.Text("bezeichner").Trim();
            if (bezeichner.Length == 0) return KiAktionsTexte.BezeichnerLeer;

            // Eine Variante entsteht nur zu einem STAMM - eine Variante der Variante
            // waere im Datenmodell eine Waise (Tab_Variante.ID_ProjektRef).
            int stammRef = new VariantenCtrl().StammRefDerVariante(idStamm);
            if (stammRef > 0)
                return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.KeinStammprojekt,
                                     idStamm, stammRef);

            // Das QUELLPROJEKT ist freiwillig - steht es aber da, muss es aufloesbar
            // sein und darf nicht der Stamm selbst sein (Auftrag #240): "Inhalt aus dem
            // Stamm" ist genau der Fall OHNE Angabe, und zwei Wege fuer dieselbe Sache
            // waeren zwei Wege, die auseinanderlaufen koennen.
            if (QuelleGenannt(a))
            {
                string quellgrund = KiHilfe.ProjektMussAufloesbarSein(a, "quellprojekt");
                if (quellgrund != null) return quellgrund;

                if (KiHilfe.ProjektId(a, "quellprojekt") == idStamm)
                    return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.QuelleIstStamm,
                                         KiHilfe.ProjektName(idStamm), idStamm);
            }

            return KiSchreibschutz.Gesperrt("Tab_Projekt", "ID", idStamm);
        }

        /// <summary>Steht im Aufruf ueberhaupt ein Quellprojekt? (Auftrag #240)</summary>
        private static bool QuelleGenannt(KiAufruf a)
        {
            return (a.Text("quellprojekt") ?? "").Trim().Length > 0;
        }

        /// <summary>
        /// Das gewaehlte QUELLPROJEKT (Auftrag #240) oder <c>0</c>, wenn keines genannt
        /// ist beziehungsweise der Stamm selbst genannt wurde. <c>0</c> bedeutet fuer
        /// <c>VariantenCtrl.AnlegenAusStamm</c> „der Stamm" - der bisherige Weg.
        /// </summary>
        private static int Quellprojekt(KiAufruf a, int idStamm)
        {
            if (!QuelleGenannt(a)) return 0;
            int id = KiHilfe.ProjektId(a, "quellprojekt");
            return id > 0 && id != idStamm ? id : 0;
        }

        // =====================================================================
        // speichervariante_aktiv_setzen
        // =====================================================================

        /// <summary>
        /// Macht genau eine Speichervariante zur aktiven Variante ihres Projekts.
        /// Andockpunkt <c>StromspeicherVarianteCtrl.SetzeAktiv(int, int)</c>
        /// (<c>Controller\StromspeicherVarianteCtrl.cs:229</c>).
        /// </summary>
        /// <remarks>
        /// UMKEHRBAR: Der Vorzustand steht in <c>ReadAktiveVariante</c> (<c>:69</c>) und
        /// wird vor der Aenderung gelesen, in der Vorschau genannt und im Ergebnis
        /// festgehalten (Fachkonzept 4.4, Punkt 3).
        /// </remarks>
        internal static KiAktion SpeichervarianteAktivSetzen()
        {
            return new KiAktion(
                name: "speichervariante_aktiv_setzen",
                zweck: KiAktionsTexte.ZweckSpeichervarianteAktiv,
                titel: KiAktionsTexte.TitelSpeichervarianteAktiv,
                beispiel: KiAktionsTexte.BeispielSpeichervarianteAktiv,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "StromspeicherVarianteCtrl.SetzeAktiv",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(),
                    new KiParameter("speichervariante", KiParameterTyp.Text,
                                    KiAktionsTexte.ErlVarianteId,
                                    anzeigename: KiAktionsTexte.VarianteIdName, maxLaenge: 120)
                },
                vorbedingung: a => SpeichervarianteVorbedingung(a),
                vorschau: a =>
                {
                    int idProjekt = KiHilfe.ProjektId(a);
                    int idVariante = SpeichervarianteWaehlen(a).Id;

                    StromspeicherVarianteModel aktiv = new StromspeicherVarianteCtrl().ReadAktiveVariante(idProjekt);
                    StromspeicherVarianteModel ziel = Speichervariante(idProjekt, idVariante);

                    string vorher = aktiv != null
                        ? aktiv.ID.ToString(CultureInfo.CurrentCulture)
                        : KiAktionsTexte.SpeichervarianteKeineAktive;

                    int andere = new StromspeicherVarianteCtrl().ReadAllByProjekt(idProjekt).Count - 1;

                    return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.VorschauSpeichervariante,
                                         vorher, idVariante,
                                         ziel != null ? ziel.ID_Energieanlage : 0,
                                         idProjekt, andere < 0 ? 0 : andere);
                },
                wirkung: KiAktionsTexte.WirkungSpeichervarianteAktiv,
                umkehrbar: true,
                ausfuehren: a =>
                {
                    int idProjekt = KiHilfe.ProjektId(a);
                    int idVariante = SpeichervarianteWaehlen(a).Id;

                    var ctrl = new StromspeicherVarianteCtrl();
                    StromspeicherVarianteModel vorher = ctrl.ReadAktiveVariante(idProjekt);
                    int idVorher = vorher != null ? vorher.ID : 0;

                    if (idVorher == idVariante)
                        return KiErgebnis.Ok(string.Format(CultureInfo.CurrentCulture,
                                                           KiAktionsTexte.SpeichervarianteSchonAktiv,
                                                           idVariante, idProjekt),
                                             null, anzahl: 0);

                    if (!new StromspeicherVarianteCtrl().SetzeAktiv(idProjekt, idVariante))
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          KiAktionsTexte.SpeichervarianteFehlgeschlagen, idVariante));

                    string datumsmeldung = AenderungsdatumSetzen(idProjekt);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_projekt", idProjekt,
                        "id_variante_neu", idVariante,
                        "id_variante_vorher", idVorher));

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SpeichervarianteGesetzt,
                                      idVariante, idProjekt,
                                      idVorher > 0 ? idVorher.ToString(CultureInfo.CurrentCulture)
                                                   : KiAktionsTexte.SpeichervarianteKeineAktive),
                        zeilen, anzahl: 1);

                    if (datumsmeldung != null) e.MitMeldungen(new[] { datumsmeldung });
                    return e;
                });
        }

        private static string SpeichervarianteVorbedingung(KiAufruf a)
        {
            string grund = KiHilfe.ProjektMussAufloesbarSein(a);
            if (grund != null) return grund;

            KiHilfe.Auswahl wahl;
            try
            {
                wahl = SpeichervarianteWaehlen(a);
            }
            catch (SqliteException ex)
            {
                return KiAktionsTexte.SpeicherTabelleFehlt + " " + ex.Message;
            }
            if (!wahl.Ok) return wahl.Fehler;

            return KiSchreibschutz.Gesperrt(StromspeicherVarianteCtrl.TABLE, "ID", wahl.Id);
        }

        /// <summary>Die Speichervariante, WENN sie zu diesem Projekt gehoert - sonst null.</summary>
        /// <summary>
        /// Speichervariante ueber die Betriebsart waehlen - der einzige Klartext,
        /// den speichervarianten_auflisten zeigt. Die Berechnungsart unterscheidet
        /// gleichlautende Betriebsarten.
        /// </summary>
        private static KiHilfe.Auswahl SpeichervarianteWaehlen(KiAufruf a)
        {
            var kandidaten = new List<KiHilfe.Kandidat>();
            foreach (StromspeicherVarianteModel v in
                     new StromspeicherVarianteCtrl().ReadAllByProjekt(KiHilfe.ProjektId(a)))
            {
                kandidaten.Add(new KiHilfe.Kandidat(v.ID, v.Betriebsart, v.Berechnungsart));
            }

            return KiHilfe.Waehle(a.Text("speichervariante"), kandidaten, KiAktionsTexte.VarianteIdName);
        }

        /// <summary>
        /// Kostenposition ueber ihre Bezeichnung waehlen, begrenzt auf das gewaehlte
        /// Projekt - eine fremde Position steht damit gar nicht erst zur Auswahl.
        /// Die Komponente unterscheidet gleichlautende Bezeichnungen.
        /// </summary>
        private static KiHilfe.Auswahl KostenpositionWaehlen(KiAufruf a)
        {
            var kandidaten = new List<KiHilfe.Kandidat>();
            int idProjekt = KiHilfe.ProjektId(a);

            if (idProjekt > 0)
            {
                DataTable dt = null;
                try
                {
                    dt = DataRepository.GetDataTable(
                        "SELECT ID, KomponentenID, StammID FROM Tab_ProjektWerte WHERE ProjektID = ?",
                        new DbParam("@id", (Int32)idProjekt));
                }
                catch { }

                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        kandidaten.Add(new KiHilfe.Kandidat(
                            Ganz(r, "ID"),
                            Text("SELECT MIN(Bezeichnung) FROM Tab_Kostenfaktor WHERE StammID = ?",
                                 Ganz(r, "StammID")),
                            Text("SELECT MIN(Komponente) FROM Tab_KostenKomponente WHERE ID = ?",
                                 Ganz(r, "KomponentenID"))));
                    }
                }
            }

            return KiHilfe.Waehle(a.Text("kostenposition"), kandidaten, KiAktionsTexte.PositionsIdName);
        }

        private static StromspeicherVarianteModel Speichervariante(int idProjekt, int idVariante)
        {
            foreach (StromspeicherVarianteModel v in new StromspeicherVarianteCtrl().ReadAllByProjekt(idProjekt))
                if (v.ID == idVariante) return v;
            return null;
        }

        // =====================================================================
        // kostenposition_setzen
        // =====================================================================

        /// <summary>
        /// Setzt den Betrag einer vorhandenen Kostenposition. Andockpunkt
        /// <c>KostenPositionCtrl.SetzeBetragNachId(int, double)</c>
        /// (<c>Controller\KostenPositionCtrl.cs:141</c>); Vorzustand ueber
        /// <c>LiesBetrag</c> (<c>:164</c>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// UMKEHRBAR: Der alte Betrag wird vor der Aenderung gelesen, in der Vorschau
        /// genannt und im Ergebnis festgehalten (Fachkonzept 4.4, Punkt 3).
        /// </para>
        /// <para>
        /// <b>Nur SETZEN, nie ANLEGEN.</b> Gerufen wird ausschliesslich
        /// <c>SetzeBetragNachId</c> - nicht <c>SetzeBetrag</c> mit
        /// <c>anlegenWennFehlt</c>. Der Weg ueber <c>SetzeBetrag</c> legt bei Bedarf einen
        /// KATALOGEINTRAG an (<c>StammIdNeben</c>, <c>:170</c>) und eine Gruppe
        /// (<c>GruppeSichern</c>, <c>:200</c>); Katalogpflege gehoert ausdruecklich nicht
        /// zu den Aufgaben des Assistenten (Fachkonzept 1.2).
        /// </para>
        /// <para>
        /// <b>Abweichung vom Katalog 5.2:</b> Dort traegt die Aktion nur
        /// <c>positions_id</c> und <c>betrag</c>. Hier kommt <c>projekt_id</c> dazu, und
        /// die Vorbedingung prueft, dass die Position wirklich zu diesem Projekt gehoert.
        /// Zwei Gruende: Eine nackte Positions-ID ist fuer den Anwender nicht nachpruefbar,
        /// und die Protokollzeile fuehrt ein Feld „Projekt", das sonst leer bliebe -
        /// gerade die Erkennbarkeit soll diese Etappe herstellen.
        /// </para>
        /// </remarks>
        internal static KiAktion KostenpositionSetzen()
        {
            return new KiAktion(
                name: "kostenposition_setzen",
                zweck: KiAktionsTexte.ZweckKostenpositionSetzen,
                titel: KiAktionsTexte.TitelKostenpositionSetzen,
                beispiel: KiAktionsTexte.BeispielKostenpositionSetzen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "KostenPositionCtrl.SetzeBetragNachId / LiesBetrag",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(),
                    new KiParameter("kostenposition", KiParameterTyp.Text,
                                    KiAktionsTexte.ErlPositionsId,
                                    anzeigename: KiAktionsTexte.PositionsIdName, maxLaenge: 200),
                    new KiParameter("betrag", KiParameterTyp.Zahl,
                                    KiAktionsTexte.ErlBetrag,
                                    anzeigename: KiAktionsTexte.BetragName,
                                    min: 0, max: 1000000000, einheit: DbWerte.KOSTEN_EINHEIT_EURO)
                },
                vorbedingung: a => KostenpositionVorbedingung(a),
                vorschau: a =>
                {
                    int idPosition = KostenpositionWaehlen(a).Id;
                    double neu = a.Zahl("betrag");
                    Kostenposition k = KostenpositionLesen(idPosition);
                    double alt = k != null ? k.Betrag : 0.0;

                    return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.VorschauKostenposition,
                                         idPosition,
                                         k != null ? k.ProjektId : 0,
                                         k != null ? k.Projektname : "",
                                         k != null ? k.Komponente : "",
                                         k != null ? k.Bezeichnung : "",
                                         alt, neu, neu - alt);
                },
                wirkung: KiAktionsTexte.WirkungKostenpositionSetzen,
                umkehrbar: true,
                ausfuehren: a =>
                {
                    int idProjekt = KiHilfe.ProjektId(a);
                    int idPosition = KostenpositionWaehlen(a).Id;
                    double neu = a.Zahl("betrag");

                    double alt = KostenPositionCtrl.LiesBetrag(idPosition);

                    if (!KostenPositionCtrl.SetzeBetragNachId(idPosition, neu))
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          KiAktionsTexte.PositionFehlgeschlagen, idPosition));

                    string datumsmeldung = AenderungsdatumSetzen(idProjekt);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_projekt", idProjekt,
                        "id_position", idPosition,
                        "betrag_vorher_eur", KiHilfe.Wert(alt),
                        "betrag_nachher_eur", KiHilfe.Wert(neu)));

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.PositionGesetzt,
                                      idPosition, neu, alt),
                        zeilen, anzahl: 1);

                    if (datumsmeldung != null) e.MitMeldungen(new[] { datumsmeldung });
                    return e;
                });
        }

        private static string KostenpositionVorbedingung(KiAufruf a)
        {
            string grund = KiHilfe.ProjektMussAufloesbarSein(a);
            if (grund != null) return grund;

            // Gewaehlt wird innerhalb des Projekts - eine fremde Position kann
            // damit gar nicht erst zur Auswahl stehen.
            KiHilfe.Auswahl wahl = KostenpositionWaehlen(a);
            if (!wahl.Ok) return wahl.Fehler;

            return KiSchreibschutz.Gesperrt("Tab_ProjektWerte", "ID", wahl.Id);
        }

        /// <summary>Die Kenndaten EINER Kostenposition - fuer Vorbedingung und Vorschau.</summary>
        private sealed class Kostenposition
        {
            internal int Id;
            internal int ProjektId;
            internal int KomponentenId;
            internal int StammId;
            internal double Betrag;
            internal string Projektname = "";
            internal string Komponente = "";
            internal string Bezeichnung = "";
        }

        /// <summary>
        /// Liest eine Kostenposition; <c>null</c>, wenn es sie nicht gibt.
        /// </summary>
        /// <remarks>
        /// Bewusst vier einfache Abfragen statt eines dreifachen LEFT JOIN: Access
        /// verlangt fuer mehrfache Verbunde eine geklammerte Schreibweise, die bei einem
        /// fehlenden Katalogeintrag schweigend die ganze Zeile verliert. Fuer eine
        /// Vorschau ist Robustheit wichtiger als die Zahl der Abfragen.
        /// </remarks>
        private static Kostenposition KostenpositionLesen(int idPosition)
        {
            if (idPosition <= 0) return null;

            DataTable dt;
            try
            {
                dt = DataRepository.GetDataTable(
                    "SELECT ID, ProjektID, KomponentenID, StammID, EingegebenerWert " +
                    "FROM Tab_ProjektWerte WHERE ID = ? LIMIT 1",
                    new DbParam("@id", (Int32)idPosition));
            }
            catch { return null; }

            if (dt == null || dt.Rows.Count == 0) return null;
            DataRow r = dt.Rows[0];

            var k = new Kostenposition
            {
                Id = Ganz(r, "ID"),
                ProjektId = Ganz(r, "ProjektID"),
                KomponentenId = Ganz(r, "KomponentenID"),
                StammId = Ganz(r, "StammID"),
                Betrag = Gleit(r, "EingegebenerWert")
            };

            k.Projektname = KiHilfe.ProjektName(k.ProjektId);
            k.Komponente = Text("SELECT MIN(Komponente) FROM Tab_KostenKomponente WHERE ID = ?", k.KomponentenId);
            k.Bezeichnung = Text("SELECT MIN(Bezeichnung) FROM Tab_Kostenfaktor WHERE StammID = ?", k.StammId);
            return k;
        }

        // =====================================================================
        // Gemeinsames
        // =====================================================================

        /// <summary>
        /// Setzt das Aenderungsdatum des Projekts - die Erkennbarkeit der Etappe 3.
        /// </summary>
        /// <remarks>
        /// Ueber <c>ProjektCtrl.ReadSingle</c> + <c>Update()</c>, also ueber den Bestand
        /// und nicht ueber ein eigenes UPDATE. Ein Fehlschlag ist eine MELDUNG und kein
        /// Abbruch: die eigentliche Aenderung ist zu diesem Zeitpunkt schon geschrieben,
        /// und ein fehlendes Datum darf sie nicht in Zweifel ziehen.
        /// </remarks>
        /// <remarks>
        /// <b>Seit dem 15.09.2026 <c>internal</c>:</b> Die fuenf nachgezogenen
        /// Schreibaktionen stehen bei ihren Verwandten (Uebernahme, Wirtschaftlichkeit)
        /// und brauchen denselben Vermerk. Eine zweite Fassung dort waere genau die
        /// Stelle, an der die Erkennbarkeit halb verlorenginge.
        /// </remarks>
        internal static string AenderungsdatumSetzen(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                var ctrl = new ProjektCtrl();
                ctrl.ReadSingle(idProjekt);
                if (ctrl.rows == 0) return null;

                ctrl.m_Aenderungsdatum = DateTime.Now;
                if (!ctrl.Update())
                    return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.AenderungsdatumFehlt, idProjekt);
                return null;
            }
            catch (Exception ex)
            {
                return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.AenderungsdatumFehlt, idProjekt) +
                       " (" + ex.Message + ")";
            }
        }

        private static int Skalar(string sql, int id)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql, new DbParam("@id", (Int32)id));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        private static string Text(string sql, int id)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql, new DbParam("@id", (Int32)id));
                return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
            }
            catch { return ""; }
        }

        private static int Ganz(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0; }
        }

        private static double Gleit(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0.0;
            try { return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture); }
            catch { return 0.0; }
        }

        // =====================================================================
        // speicherauslegung_uebernehmen  (Fachkonzept 5.2, 15.09.2026)
        // =====================================================================

        /// <summary>
        /// Traegt Kapazitaet und Leistung in den Projektdatensatz des Stromspeichers ein.
        /// Andockpunkt <c>StromspeicherSimCtrl.UebernehmeAuslegung(int, double, double)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Die Vorbedingung des Bestands ist scharf, und das mit Grund:</b> Geschrieben
        /// wird nur, wenn das Projekt GENAU EINE <c>SP_TYP</c>-Anlage fuehrt. Varianten
        /// desselben Speichers teilen sich eine Geraetekopie in <c>Tab_Stromspeicher</c> -
        /// ein Schreibzugriff aendert sonst stillschweigend auch die Geschwistervarianten.
        /// Der Bestand bricht in diesem Fall ab und legt den Grund in
        /// <c>LetzterHinweis</c>; diese Aktion reicht ihn woertlich weiter, statt ihn zu
        /// verschlucken.
        /// </para>
        /// <para>
        /// <b>UMKEHRBAR:</b> Vorher- und Nachher-Wert stehen in der Vorschau und im
        /// Ergebnis; zurueckgeschrieben wird mit demselben Aufruf und den alten Zahlen.
        /// </para>
        /// <para>
        /// <b>Nachgerechnet wird NICHTS.</b> Der Bestand tut es nicht, und diese Aktion
        /// stoesst es auch nicht an: Wann eine Simulation laeuft, entscheidet der Anwender
        /// (<c>simulation_rechnen</c>, Stufe 3, eigene Bestaetigung).
        /// </para>
        /// </remarks>
        internal static KiAktion SpeicherauslegungUebernehmen()
        {
            return new KiAktion(
                name: "speicherauslegung_uebernehmen",
                zweck: KiAktionsTexte.ZweckSpeicherauslegung,
                titel: KiAktionsTexte.TitelSpeicherauslegung,
                beispiel: KiAktionsTexte.BeispielSpeicherauslegung,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "StromspeicherSimCtrl.UebernehmeAuslegung",
                wirkung: KiAktionsTexte.WirkungSpeicherauslegung,
                umkehrbar: true,
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(pflicht: false),
                    new KiParameter("kapazitaet_kwh", KiParameterTyp.Zahl,
                                    KiAktionsTexte.ErlKapazitaet,
                                    anzeigename: KiAktionsTexte.KapazitaetName,
                                    min: 0, max: 1000000, einheit: "kWh"),
                    new KiParameter("leistung_kw", KiParameterTyp.Zahl,
                                    KiAktionsTexte.ErlLeistung,
                                    anzeigename: KiAktionsTexte.LeistungName,
                                    min: 0, max: 1000000, einheit: "kW")
                },
                vorbedingung: a =>
                {
                    string grund = KiHilfe.ProjektMussAufloesbarSein(a);
                    if (grund != null) return grund;

                    int id = KiHilfe.ProjektId(a);

                    // GENAU EINE Speicheranlage - dieselbe Bedingung, an der der Bestand
                    // sonst erst beim Schreiben abbraeche. Hier faellt sie VOR der
                    // Bestaetigung auf, und der Anwender klickt nichts weg, was ohnehin
                    // nicht laufen kann.
                    int anzahl = WErzeugerCtrl.AnlagenJeTyp(id, WizardItemClass.SP_TYP).Count;
                    if (anzahl == 0)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.SpeicherKeineAnlage, KiHilfe.ProjektName(id));
                    if (anzahl > 1)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.SpeicherMehrereAnlagen, anzahl);

                    return KiSchreibschutz.Gesperrt("Tab_Projekt", "ID", id);
                },
                vorschau: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    Auslegung alt = AuslegungLesen(id);

                    var aenderungen = new List<KiFeldAenderung>
                    {
                        new KiFeldAenderung(KiAktionsTexte.KapazitaetName,
                                            Menge(alt.KapazitaetKwh, "kWh"),
                                            Menge(a.Zahl("kapazitaet_kwh"), "kWh")),
                        new KiFeldAenderung(KiAktionsTexte.LeistungName,
                                            Menge(alt.LeistungKw, "kW"),
                                            Menge(a.Zahl("leistung_kw"), "kW"))
                    };

                    return KiFeldBlock.Felder(KiHilfe.ProjektName(id), aenderungen);
                },
                ausfuehren: a =>
                {
                    int id = KiHilfe.ProjektId(a);
                    double kapazitaet = a.Zahl("kapazitaet_kwh");
                    double leistung = a.Zahl("leistung_kw");

                    Auslegung alt = AuslegungLesen(id);

                    var ctrl = new StromspeicherSimCtrl();
                    if (!ctrl.UebernehmeAuslegung(id, kapazitaet, leistung))
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          KiAktionsTexte.SpeicherauslegungFehlgeschlagen,
                                          KiHilfe.Text(ctrl.LetzterHinweis)));

                    string datumsmeldung = AenderungsdatumSetzen(id);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_projekt", id,
                        "kapazitaet_vorher_kwh", KiHilfe.Wert(alt.KapazitaetKwh),
                        "kapazitaet_nachher_kwh", KiHilfe.Wert(kapazitaet),
                        "leistung_vorher_kw", KiHilfe.Wert(alt.LeistungKw),
                        "leistung_nachher_kw", KiHilfe.Wert(leistung)));

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.SpeicherauslegungGesetzt,
                                      kapazitaet, leistung, KiHilfe.ProjektName(id)),
                        zeilen, anzahl: 1);

                    var meldungen = new List<string>();
                    if (!string.IsNullOrWhiteSpace(ctrl.LetzterHinweis)) meldungen.Add(ctrl.LetzterHinweis.Trim());
                    if (datumsmeldung != null) meldungen.Add(datumsmeldung);
                    return e.MitMeldungen(meldungen);
                });
        }

        /// <summary>Der Auslegungsstand einer Speicheranlage - fuer Vorschau und Rueckweg.</summary>
        private struct Auslegung
        {
            internal double KapazitaetKwh;
            internal double LeistungKw;
        }

        /// <summary>
        /// Kapazitaet und Leistung des Projektspeichers; 0/0, wenn keiner lesbar ist.
        /// </summary>
        /// <remarks>
        /// Gelesen wird ueber <c>Tab_Energieanlagen.ID_SP</c> - genau der Verweis, ueber
        /// den auch <c>UebernehmeAuslegung</c> schreibt. Ein zweiter Leseweg koennte einen
        /// anderen Satz treffen als der Schreibweg.
        /// </remarks>
        private static Auslegung AuslegungLesen(int idProjekt)
        {
            var stand = new Auslegung();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT s.Energie, s.Leistung FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_Stromspeicher AS s ON a.ID_SP = s.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_SP IS NOT NULL",
                    new DbParam("@p", idProjekt));

                if (dt != null && dt.Rows.Count > 0)
                {
                    stand.KapazitaetKwh = Gleit(dt.Rows[0], "Energie");
                    stand.LeistungKw = Gleit(dt.Rows[0], "Leistung");
                }
            }
            catch { }
            return stand;
        }

        /// <summary>Ein Zahlwert mit Einheit fuer den Bestaetigungsblock.</summary>
        private static string Menge(double wert, string einheit)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture) + " " + einheit;
        }

        // =====================================================================
        // speichervariante_anlegen  (Fachkonzept 5.2, 15.09.2026)
        // =====================================================================

        /// <summary>
        /// Legt die Betriebsvariante einer Speicheranlage an. Andockpunkt
        /// <c>StromspeicherVarianteCtrl.Insert(StromspeicherVarianteModel)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>IDEMPOTENT - und die Bestaetigung sagt das.</b> Fuehrt die Anlage bereits
        /// eine Variante, liefert <c>Insert</c> deren Id zurueck und schreibt NICHTS. Das
        /// ist keine Fehlerbehandlung, sondern die Zusage des Bestands; die Vorbedingung
        /// faengt den Fall trotzdem vorher ab, damit niemand eine Bestaetigung fuer eine
        /// Aenderung wegklickt, die gar nicht stattfindet.
        /// </para>
        /// <para>
        /// <b>Vier Werte, sonst Vorgaben.</b> Das Modell setzt Betriebsart und SoC-Band;
        /// alles Weitere kommt aus den Vorgaben des Modells
        /// (<c>StromspeicherVarianteModel</c>). Die Betriebsart ist eine AUFZAEHLUNG aus
        /// <see cref="DbWerte"/> - ein freier Text landete sonst als Persistenzwert in
        /// der Tabelle.
        /// </para>
        /// <para>
        /// NICHT umkehrbar: <c>StromspeicherVarianteCtrl.Delete</c> steht ausdruecklich
        /// nicht im Register (Fachkonzept 5.4).
        /// </para>
        /// </remarks>
        internal static KiAktion SpeichervarianteAnlegen()
        {
            return new KiAktion(
                name: "speichervariante_anlegen",
                zweck: KiAktionsTexte.ZweckSpeichervarianteAnlegen,
                titel: KiAktionsTexte.TitelSpeichervarianteAnlegen,
                beispiel: KiAktionsTexte.BeispielSpeichervarianteAnlegen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "StromspeicherVarianteCtrl.Insert",
                wirkung: KiAktionsTexte.WirkungSpeichervarianteAnlegen,
                umkehrbar: false,
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(pflicht: false),
                    new KiParameter("speicheranlage", KiParameterTyp.Text,
                                    KiAktionsTexte.ErlSpeicheranlage, pflicht: false,
                                    anzeigename: KiAktionsTexte.SpeicheranlageName, maxLaenge: 120),
                    new KiParameter("betriebsart", KiParameterTyp.Aufzaehlung,
                                    KiAktionsTexte.ErlBetriebsart, pflicht: false,
                                    anzeigename: KiAktionsTexte.BetriebsartName,
                                    werte: new[] { DbWerte.SP_BETRIEBSART_GRUENSTROM,
                                                   DbWerte.SP_BETRIEBSART_GRAUSTROM }),
                    new KiParameter("soc_min_prozent", KiParameterTyp.Zahl,
                                    KiAktionsTexte.ErlSocMin, pflicht: false,
                                    anzeigename: KiAktionsTexte.SocMinName, min: 0, max: 100, einheit: "%"),
                    new KiParameter("soc_max_prozent", KiParameterTyp.Zahl,
                                    KiAktionsTexte.ErlSocMax, pflicht: false,
                                    anzeigename: KiAktionsTexte.SocMaxName, min: 0, max: 100, einheit: "%")
                },
                vorbedingung: a =>
                {
                    string grund = KiHilfe.ProjektMussAufloesbarSein(a);
                    if (grund != null) return grund;

                    KiHilfe.Auswahl w = SpeicheranlageWaehlen(a);
                    if (w.Fehler != null) return w.Fehler;

                    if (new StromspeicherVarianteCtrl().ReadByEnergieanlage(w.Id) != null)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.SpeichervarianteSchonDa, w.Name);

                    double min = a.Zahl("soc_min_prozent", StromspeicherVarianteModel.SOC_MIN_VORGABE);
                    double max = a.Zahl("soc_max_prozent", StromspeicherVarianteModel.SOC_MAX_VORGABE);
                    if (min >= max)
                        return string.Format(CultureInfo.CurrentCulture,
                                             KiAktionsTexte.SocBandVerdreht, min, max);

                    return KiSchreibschutz.Gesperrt("Tab_Projekt", "ID", KiHilfe.ProjektId(a));
                },
                vorschau: a =>
                {
                    KiHilfe.Auswahl w = SpeicheranlageWaehlen(a);

                    return string.Format(CultureInfo.CurrentCulture,
                        KiAktionsTexte.VorschauSpeichervarianteAnlegen,
                        w.Name, w.Id,
                        KiHilfe.ProjektName(KiHilfe.ProjektId(a)),
                        Betriebsart(a),
                        a.Zahl("soc_min_prozent", StromspeicherVarianteModel.SOC_MIN_VORGABE),
                        a.Zahl("soc_max_prozent", StromspeicherVarianteModel.SOC_MAX_VORGABE));
                },
                ausfuehren: a =>
                {
                    int idProjekt = KiHilfe.ProjektId(a);
                    KiHilfe.Auswahl w = SpeicheranlageWaehlen(a);
                    if (w.Fehler != null) return KiErgebnis.Abgelehnt(w.Fehler);

                    var m = new StromspeicherVarianteModel
                    {
                        ID_Energieanlage = w.Id,
                        Betriebsart = Betriebsart(a),
                        SoC_Min_Prozent = a.Zahl("soc_min_prozent",
                                                 StromspeicherVarianteModel.SOC_MIN_VORGABE),
                        SoC_Max_Prozent = a.Zahl("soc_max_prozent",
                                                 StromspeicherVarianteModel.SOC_MAX_VORGABE)
                    };

                    int neueId = new StromspeicherVarianteCtrl().Insert(m);
                    if (neueId <= 0)
                        return KiErgebnis.Fehlgeschlagen(
                            string.Format(CultureInfo.CurrentCulture,
                                          KiAktionsTexte.SpeichervarianteAnlegenFehlgeschlagen, w.Name));

                    string datumsmeldung = AenderungsdatumSetzen(idProjekt);

                    var zeilen = KiHilfe.Liste();
                    zeilen.Add(KiHilfe.Zeile(
                        "id_projekt", idProjekt,
                        "id_variante", neueId,
                        "id_energieanlage", w.Id,
                        "anlage", KiHilfe.Text(w.Name),
                        "betriebsart", m.Betriebsart,
                        "soc_min_prozent", KiHilfe.Wert(m.SoC_Min_Prozent),
                        "soc_max_prozent", KiHilfe.Wert(m.SoC_Max_Prozent)));

                    KiErgebnis e = KiErgebnis.Ok(
                        string.Format(CultureInfo.CurrentCulture,
                                      KiAktionsTexte.SpeichervarianteAngelegt, neueId, w.Name),
                        zeilen, anzahl: 1);

                    if (datumsmeldung != null) e.MitMeldungen(new[] { datumsmeldung });
                    return e;
                });
        }

        /// <summary>Die Betriebsart des Aufrufs oder die Vorgabe des Modells.</summary>
        private static string Betriebsart(KiAufruf a)
        {
            string genannt = (a.Text("betriebsart") ?? "").Trim();
            return genannt.Length > 0 ? genannt : DbWerte.SP_BETRIEBSART_GRUENSTROM;
        }

        /// <summary>
        /// Die gemeinte SPEICHERANLAGE des Projekts - genannt oder, bei genau einer, die
        /// eine.
        /// </summary>
        /// <remarks>
        /// <b>Gewaehlt wird ueber den Bezeichner</b>, wie ueberall im Register: Der
        /// Anwender kennt den Namen seiner Anlage, die Datensatznummer sieht er nirgends.
        /// Fuehrt das Projekt genau eine Speicheranlage, darf der Name ganz entfallen -
        /// dann gibt es nichts auszuwaehlen.
        /// </remarks>
        private static KiHilfe.Auswahl SpeicheranlageWaehlen(KiAufruf a)
        {
            int idProjekt = KiHilfe.ProjektId(a);
            List<WErzeugerCtrl.AnlagenZeile> anlagen =
                WErzeugerCtrl.AnlagenJeTyp(idProjekt, WizardItemClass.SP_TYP);

            var kandidaten = new List<KiHilfe.Kandidat>();
            foreach (WErzeugerCtrl.AnlagenZeile z in anlagen)
                kandidaten.Add(new KiHilfe.Kandidat(z.Id, z.Bezeichner, ""));

            string genannt = (a.Text("speicheranlage") ?? "").Trim();
            if (genannt.Length == 0 && kandidaten.Count == 1)
                return new KiHilfe.Auswahl { Id = kandidaten[0].Id, Name = kandidaten[0].Name };

            return KiHilfe.Waehle(genannt, kandidaten, KiAktionsTexte.SpeicheranlageName);
        }
    }
}
