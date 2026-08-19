using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Schreibaktionen der Stufe 2 (Fachkonzept 5.2, Etappe 3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Drei Aktionen, nicht acht.</b> Aus dem Katalog 5.2 sind die drei uebernommen,
    /// die sich vollstaendig absichern lassen: Sie schreiben in EINE Tabelle, ihr
    /// Vorzustand ist lesbar, und ihr Andockpunkt bringt seine Regeln selbst mit. Die
    /// uebrigen (Komponenten- und Merkmaluebernahme, Wirtschaftlichkeitsparameter,
    /// Speicherauslegung) haengen an mehrstufigen Trockenlaeufen oder an einer
    /// Mehrdeutigkeit, die eine Rueckfrage braucht - beides gehoert in eine eigene Runde
    /// mit eigener Abnahme.
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
        /// <c>VariantenCtrl.AnlegenAusStamm(int, string, string, out string)</c>
        /// (<c>Controller\VariantenCtrl.cs:105</c>), der intern
        /// <c>ProjektDuplizierenCtrl.Duplizieren</c> nutzt.
        /// </summary>
        /// <remarks>
        /// NICHT umkehrbar: <c>VariantenCtrl.LoescheVariante</c> steht ausdruecklich nicht
        /// im Register (Fachkonzept 5.4). Das sagt die Bestaetigung so.
        /// </remarks>
        internal static KiAktion VarianteAnlegen()
        {
            return new KiAktion(
                name: "variante_anlegen",
                zweck: KiAktionsTexte.ZweckVarianteAnlegen,
                stufe: Schutzstufe.Schreiben,
                andockpunkt: "VariantenCtrl.AnlegenAusStamm",
                parameter: new[]
                {
                    KiHilfe.ProjektParameter(KiAktionsTexte.ErlStammId, name: "stammprojekt",
                                             anzeigename: KiAktionsTexte.StammIdName),
                    new KiParameter("bezeichner", KiParameterTyp.Text,
                                    KiAktionsTexte.ErlBezeichner,
                                    anzeigename: KiAktionsTexte.BezeichnerName, maxLaenge: 60)
                },
                vorbedingung: a => VarianteVorbedingung(a),
                vorschau: a =>
                {
                    int idStamm = KiHilfe.ProjektId(a, "stammprojekt");
                    string stammName = KiHilfe.ProjektName(idStamm);
                    string bezeichner = a.Text("bezeichner").Trim();
                    int anlagen = Skalar("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ?", idStamm);

                    return string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.VorschauVarianteAnlegen,
                                         stammName, idStamm, bezeichner,
                                         NeuerProjektname(stammName, bezeichner), anlagen);
                },
                wirkung: KiAktionsTexte.WirkungVarianteAnlegen,
                umkehrbar: false,
                ausfuehren: a =>
                {
                    int idStamm = KiHilfe.ProjektId(a, "stammprojekt");
                    string stammName = KiHilfe.ProjektName(idStamm);
                    string bezeichner = a.Text("bezeichner").Trim();

                    var ctrl = new VariantenCtrl();
                    string fehler;
                    int neueId = ctrl.AnlegenAusStamm(idStamm, stammName, bezeichner, out fehler);

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

            return KiSchreibschutz.Gesperrt("Tab_Projekt", "ID", idStamm);
        }

        /// <summary>
        /// Der Projektname, den <c>VariantenCtrl.AnlegenAusStamm</c> vergeben WUERDE.
        /// </summary>
        /// <remarks>
        /// Das ist bewusst ein Spiegel der Namensbildung aus
        /// <c>Controller\VariantenCtrl.cs:113-118</c> und keine zweite Regel: Die Vorschau
        /// muss den Namen nennen, der hinterher wirklich dasteht, sonst bestaetigt der
        /// Anwender etwas anderes, als er bekommt. Der Aktionsharnisch vergleicht Vorschau
        /// und Ergebnis Zeichen fuer Zeichen - laufen die beiden Stellen auseinander,
        /// faellt es dort auf.
        /// </remarks>
        private static string NeuerProjektname(string stammName, string bezeichner)
        {
            var ctrl = new VariantenCtrl();
            string basisName = (stammName ?? "") + " - " + (bezeichner ?? "");
            string neuerName = basisName;
            int n = 2;
            try
            {
                while (ctrl.ProjektnameExistiert(neuerName)) { neuerName = basisName + " (" + n + ")"; n++; }
            }
            catch { return basisName; }
            return neuerName;
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
            catch (OleDbException ex)
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
                        new OleDbParameter("@id", (Int32)idProjekt));
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
                    "SELECT TOP 1 ID, ProjektID, KomponentenID, StammID, EingegebenerWert " +
                    "FROM Tab_ProjektWerte WHERE ID = ?",
                    new OleDbParameter("@id", (Int32)idPosition));
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
        private static string AenderungsdatumSetzen(int idProjekt)
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
                object o = DataRepository.ExecuteScalar(sql, new OleDbParameter("@id", (Int32)id));
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        private static string Text(string sql, int id)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql, new OleDbParameter("@id", (Int32)id));
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
    }
}
