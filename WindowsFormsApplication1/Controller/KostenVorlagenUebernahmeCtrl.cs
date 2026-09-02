using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>Ergebnis einer Übernahme (Zähler + Klartextmeldungen).</summary>
    public sealed class UebernahmeErgebnis
    {
        public int Angelegt;
        public int Uebersprungen;
        public bool Fehler;
        public readonly List<string> Meldungen = new List<string>();
    }

    /// <summary>
    /// Übernahme-Mechanik Stamm → Projekt (Etappe KD3, Konzept Kostendialoge Rev. 1.2,
    /// § 8): materialisiert Vorlagenpositionen als normale Projektpositionen in
    /// <c>Tab_ProjektWerte</c> (KL3) bzw. kopiert Positionen aus einem anderen Projekt.
    ///
    /// <para><b>Regeln:</b> vorhandene Projektzeilen bleiben IMMER unberührt (Muster
    /// <c>Nebenmodus.NurAnlegen</c> — kein stilles Überschreiben); die Herkunft wird in
    /// <c>Tab_ProjektWerte.VorlageID</c> vermerkt (§ 4.2 — reine Anzeige, NIE stille
    /// Kopplung); geschrieben wird über die bestehenden Wege
    /// (<see cref="KostenPositionCtrl.SetzeBetrag"/> /
    /// <see cref="KostenPositionCtrl.SetzeBetragMitZusatz"/>), damit Rechenkern und
    /// <c>Form_Kosten</c> exakt lesen, was auch eine Handeingabe erzeugt hätte
    /// (Abnahmekriterium KD3: Ergebnisgleichheit).</para>
    ///
    /// <para><b>Feldsemantik je Bemessung</b> (aus <c>BetriebskostenCtrl.Betrag</c>):
    /// absolute Bemessungen (fester Betrag/Jahresbetrag) tragen den Satz als
    /// <c>EingegebenerWert</c>, <c>Einheitpreis</c> bleibt leer; satzbasierte tragen den
    /// Satz in <c>Einheitpreis</c>, <c>EingegebenerWert</c> = 0 und <c>Menge</c> = NULL —
    /// der Betrag entsteht erst, wenn die Bezugsgröße im Projektfluss gepflegt wird.</para>
    /// </summary>
    public static class KostenVorlagenUebernahmeCtrl
    {
        // -------------------------------------------------------------- Auskunft ---

        /// <summary>Alle Projekte (ID, Projektname) für die Zielauswahl.</summary>
        public static IList<KeyValuePair<int, string>> Projekte()
        {
            var liste = new List<KeyValuePair<int, string>>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Projektname FROM Tab_Projekt ORDER BY Projektname, ID");
            foreach (DataRow r in dt.Rows)
                liste.Add(new KeyValuePair<int, string>(
                    Convert.ToInt32(r[0]), Convert.ToString(r[1])));
            return liste;
        }

        /// <summary>Vorhandene Positionen der Komponente+Kategorie im Zielprojekt
        /// (Grundlage der Klartext-Vorschau).</summary>
        public static int VorhandeneImProjekt(int projektId, int komponentenId, int kategorieId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte " +
                "WHERE ProjektID = ? AND KomponentenID = ? AND KategorieID = ?",
                new DbParam("@p", projektId),
                new DbParam("@c", komponentenId),
                new DbParam("@k", kategorieId));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        /// <summary>Ä20: Dublettensuche je Anlage (NULL-tolerant über die Vorsorge).</summary>
        private static int FindePositionAnlage(int projektId, int kategorieId,
                                               int komponentenId, int stammId, int idAnlage)
        {
            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa)
                return KostenPositionCtrl.FindePosition(projektId, kategorieId,
                                                        komponentenId, stammId);
            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_ProjektWerte WHERE ProjektID = ? AND " +
                "KategorieID = ? AND KomponentenID = ? AND StammID = ? AND ID_Anlage = ?",
                new DbParam("@p", projektId),
                new DbParam("@g", kategorieId),
                new DbParam("@k", komponentenId),
                new DbParam("@s", stammId),
                new DbParam("@a", idAnlage));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        // ------------------------------------------------- Vorlage -> Projekt ---

        /// <summary>
        /// Übernimmt eine Stammvorlage (Standard oder Variante) in ein Projekt.
        /// </summary>
        public static UebernahmeErgebnis AusVorlage(int projektId, KostenVorlageKopf vorlage)
        {
            return AusVorlage(projektId, vorlage, 0);
        }

        /// <summary>Ä20: Übernahme in EINE Anlage — der NurAnlegen-Dublettencheck
        /// läuft dann je Anlage (dieselbe Position darf an einer zweiten Anlage
        /// erneut entstehen), und jede neue Zeile trägt die Anlagenzeile.</summary>
        public static UebernahmeErgebnis AusVorlage(int projektId, KostenVorlageKopf vorlage,
                                                    int idAnlage)
        {
            return AusVorlage(projektId, vorlage, idAnlage, false);
        }

        /// <summary>ETAPPE H3 (H1-3): <paramref name="nurPflicht"/> übernimmt
        /// ausschließlich die Pflichtpositionen der Vorlage — der Weg der
        /// Auto-Anlage (<see cref="PflichtpositionenSicherstellen"/>).</summary>
        public static UebernahmeErgebnis AusVorlage(int projektId, KostenVorlageKopf vorlage,
                                                    int idAnlage, bool nurPflicht)
        {
            var e = new UebernahmeErgebnis();
            if (vorlage == null || projektId <= 0)
            {
                e.Fehler = true;
                e.Meldungen.Add("Keine Vorlage oder kein Zielprojekt gewählt.");
                return e;
            }

            string gruppe = vorlage.KategorieId == DbWerte.KOSTEN_KATEGORIE_BETRIEB
                ? DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI
                : DbWerte.KOSTEN_GRUPPE_ALLGEMEIN;

            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(vorlage.Id))
            {
                if (nurPflicht && !p.IstPflicht) continue;   // H3: Auto-Anlage legt nur Pflicht an

                int stammId = StammIdSicher(p.Bezeichnung);
                if (stammId <= 0)
                {
                    e.Fehler = true;
                    e.Meldungen.Add("Positionslexikon: \"" + p.Bezeichnung + "\" nicht anlegbar.");
                    continue;
                }

                // Vorhandene Zeile bleibt unberührt (NurAnlegen-Muster) — Ä20:
                // mit Anlagenbezug zählt nur eine Dublette AN DERSELBEN Anlage.
                if (idAnlage > 0
                    ? FindePositionAnlage(projektId, vorlage.KategorieId,
                                          vorlage.KomponentenId, stammId, idAnlage) > 0
                    : KostenPositionCtrl.FindePosition(projektId, vorlage.KategorieId,
                          vorlage.KomponentenId, stammId) > 0)
                {
                    e.Uebersprungen++;
                    continue;
                }

                BemessungKatalog.Info info = BemessungKatalog.Finde(p.Bemessung);
                bool absolut = info != null && info.Absolut;
                double startBetrag = absolut && p.Satz.HasValue ? p.Satz.Value : 0.0;

                // Ä25: MIT Anlagenbezug schreiben. Der Dublettencheck oben lief seit
                // Ä20 je Anlage, das SCHREIBEN aber weiter anlagenblind — bei einer
                // zweiten Anlage derselben Komponente (Regelfall Pufferspeicher) traf
                // SetzeBetrag die Position der ERSTEN Anlage, überschrieb ihren Betrag
                // mit dem Vorlagen-Startwert und AnlageZuordnen unten hängte sie an die
                // Ziel-Anlage um. Genau so verschwanden erfasste Pufferkosten.
                int id = KostenPositionCtrl.SetzeBetrag(projektId, vorlage.KategorieId,
                    vorlage.KomponentenId, stammId, startBetrag, gruppe, true, idAnlage);
                if (id <= 0)
                {
                    e.Fehler = true;
                    e.Meldungen.Add("Projektzeile \"" + p.Bezeichnung + "\" nicht anlegbar.");
                    continue;
                }

                var zusatz = new KostenPositionCtrl.Zusatz
                {
                    Kostenart = p.Kostenart ?? "",
                    Bemessung = string.IsNullOrEmpty(p.Bemessung)
                        ? DbWerte.BEMESSUNG_BETRAG : p.Bemessung,
                    IstErloes = p.IstErloes,
                    Menge = null,
                    Einheitpreis = absolut ? (double?)null : p.Satz,
                };
                if (!KostenPositionCtrl.SetzeBetragMitZusatz(id, startBetrag, zusatz))
                {
                    e.Fehler = true;
                    e.Meldungen.Add("Zusatzangaben zu \"" + p.Bezeichnung + "\" nicht schreibbar.");
                }

                HerkunftUndNutzungsdauer(id, vorlage.Id, p.Nutzungsdauer);
                if (idAnlage > 0) KostenProjektPositionenCtrl.AnlageZuordnen(id, idAnlage);
                // ETAPPE H3: Das Pflichtmerkmal wandert bei JEDER Übernahme mit —
                // die H1-Saat markierte nur den Bestand; ohne die Durchreichung
                // liefe die Löschsperre (H1-2) an neuen Zeilen ins Leere.
                if (p.IstPflicht) KostenProjektPositionenCtrl.PflichtSetzen(id, true);
                e.Angelegt++;
            }

            e.Meldungen.Add(e.Angelegt + " Positionen aus \"" + vorlage.Name +
                            "\" angelegt, " + e.Uebersprungen + " bereits vorhanden.");
            return e;
        }

        /// <summary>
        /// ETAPPE H3 (H1-3): stellt an JEDER Anlagenzeile des Projekts die
        /// Pflichtpositionen der Standard-Betriebskostenvorlage ihrer Komponente
        /// sicher (Muster <c>Nebenmodus.NurAnlegen</c> — vorhandene Zeilen bleiben
        /// unberührt, der Dublettencheck läuft seit Ä20 je Anlage).
        ///
        /// <para>Aufgerufen aus <c>WizardCtrl.Add_WP_Waermeerzeuger</c> NACH
        /// <c>ZuordnungReparieren</c>/<c>AnkerNachziehen</c>: Erst dann hängen die
        /// Bestandspositionen wieder an den neuen Anlagen-Ids des
        /// Del+Add-Speicherwegs, und der Check erkennt sie. ERGEBNISNEUTRAL:
        /// Vorlagen tragen keine Sätze (KL-Regel „Struktur, nicht Preise") — jede
        /// neue Zeile steht auf 0 €/a, bis der Anwender pflegt.</para>
        ///
        /// <para>Referenzanlagen (ID_Type 5–9) bekommen bewusst KEINE Positionen —
        /// die Kostenvorlagen gehören zu den sieben Projekt-Komponenten (Ä7).</para>
        /// </summary>
        /// <returns>Zahl der neu angelegten Positionen.</returns>
        public static int PflichtpositionenSicherstellen(int projektId)
        {
            if (projektId <= 0) return 0;

            int angelegt = 0;
            var vorlageJeKomponente = new Dictionary<int, KostenVorlageKopf>();

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, ID_Type FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                new DbParam("@p", projektId));
            if (dt == null) return 0;

            foreach (DataRow r in dt.Rows)
            {
                if (r["ID"] == DBNull.Value || r["ID_Type"] == DBNull.Value) continue;
                int idAnlage = Convert.ToInt32(r["ID"]);
                int komponentenId = KomponenteZuTyp(Convert.ToInt32(r["ID_Type"]));
                if (komponentenId <= 0) continue;

                KostenVorlageKopf vorlage;
                if (!vorlageJeKomponente.TryGetValue(komponentenId, out vorlage))
                {
                    vorlage = StandardVorlage(komponentenId, DbWerte.KOSTEN_KATEGORIE_BETRIEB);
                    vorlageJeKomponente[komponentenId] = vorlage;   // auch „keine" merken
                }
                if (vorlage == null) continue;

                try { angelegt += AusVorlage(projektId, vorlage, idAnlage, true).Angelegt; }
                catch { }
            }
            return angelegt;
        }

        /// <summary>Standard-Vorlage einer Komponente und Kategorie; null = keine.</summary>
        private static KostenVorlageKopf StandardVorlage(int komponentenId, int kategorieId)
        {
            foreach (KostenVorlageKopf k in KostenVorlagenCtrl.Vorlagen(komponentenId, kategorieId))
                if (k.IstStandard) return k;
            return null;
        }

        /// <summary>
        /// <c>Tab_Energieanlagen.ID_Type</c> (<see cref="WizardItemClass"/>) →
        /// <c>Tab_KostenKomponente.ID</c> — die festen Nummern 1…7, Begründung bei
        /// <c>Form_Kosten.GetKomponentenID</c>. 0 = keine Kostenkomponente
        /// (Referenztypen 5–9, unbekannte Typen).
        /// </summary>
        private static int KomponenteZuTyp(int idType)
        {
            switch (idType)
            {
                case WizardItemClass.WP_TYP: return 1;       // Wärmepumpe
                case WizardItemClass.SOLAR_TYP: return 4;    // Solarthermie
                case WizardItemClass.PV_TYP: return 3;       // Photovoltaik
                case WizardItemClass.SP_TYP: return 5;       // Stromspeicher
                case WizardItemClass.KESSEL_TYP: return 2;   // Heizkessel
                case WizardItemClass.BHKW_TYP: return 7;     // BHKW
                case WizardItemClass.PUFFER_TYP: return 6;   // Pufferspeicher
                default: return 0;
            }
        }

        // ------------------------------------------------- Projekt -> Projekt ---

        /// <summary>
        /// Kopiert die Positionen einer Komponente+Kategorie aus einem anderen Projekt
        /// (§ 8: Stammprojekt oder Projektvariante als Quelle) — feldgleich inklusive
        /// Szenariowerten und Nutzungsdauern; vorhandene Zielzeilen bleiben unberührt.
        /// </summary>
        public static UebernahmeErgebnis AusProjekt(int zielProjektId, int quellProjektId,
                                                    int komponentenId, int kategorieId)
        {
            return AusProjekt(zielProjektId, quellProjektId, komponentenId, kategorieId, -1, 0);
        }

        /// <summary>Ä21: Übernahme von einer QUELL-ANLAGE (auch desselben
        /// Projekts — „weitere Wärmepumpe aus der vorhandenen befüllen“) in die
        /// ZIEL-Anlage. quellAnlage: &gt;0 = nur diese Anlage, 0 = Positionen ohne
        /// Zuordnung, -1 = alle (Bestandsverhalten).</summary>
        public static UebernahmeErgebnis AusProjekt(int zielProjektId, int quellProjektId,
                                                    int komponentenId, int kategorieId,
                                                    int quellAnlage, int zielAnlage)
        {
            var e = new UebernahmeErgebnis();
            bool anlagenVerschieden = quellAnlage > 0 && zielAnlage > 0 && quellAnlage != zielAnlage;
            if (zielProjektId <= 0 || quellProjektId <= 0 ||
                (zielProjektId == quellProjektId && !anlagenVerschieden))
            {
                e.Fehler = true;
                e.Meldungen.Add("Quelle und Ziel müssen verschiedene Projekte oder " +
                                "verschiedene Anlagen sein.");
                return e;
            }

            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }

            string quellFilter = "";
            var quellParameter = new List<DbParam>
            {
                new DbParam("@p", quellProjektId),
                new DbParam("@c", komponentenId),
                new DbParam("@k", kategorieId)
            };
            if (spalteDa && quellAnlage > 0)
            {
                quellFilter = " AND ID_Anlage = ?";
                quellParameter.Add(new DbParam("@a", quellAnlage));
            }
            else if (spalteDa && quellAnlage == 0)
            {
                // Projekt-Id als LITERAL — ACE-Unterabfragen-Falle (Ä21-Befund).
                quellFilter = " AND (ID_Anlage IS NULL OR ID_Anlage NOT IN " +
                              "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " +
                              quellProjektId + "))";
            }

            DataTable dt = DataRepository.GetDataTable(
                "SELECT StammID, EingegebenerWert, BestCase, WorstCase, Nutzungsdauer, " +
                "BestCase_Nutzungsdauer, WorstCase_Nutzungsdauer, Einheit, Gruppe, [" +
                SchemaKatalog.SPALTE_PW_KOSTENART + "], [" +
                SchemaKatalog.SPALTE_PW_BEMESSUNG + "], [" +
                SchemaKatalog.SPALTE_PW_IST_ERLOES + "], [" +
                SchemaKatalog.SPALTE_PW_MENGE + "], [" +
                SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], [" +
                SchemaKatalog.SPALTE_PW_VORLAGEID + "], [" +
                SchemaKatalog.SPALTE_PW_STARTJAHR + "] " +
                "FROM Tab_ProjektWerte WHERE ProjektID = ? AND KomponentenID = ? AND KategorieID = ?" +
                quellFilter,
                quellParameter.ToArray());

            foreach (DataRow r in dt.Rows)
            {
                if (r["StammID"] == DBNull.Value) continue;
                int stammId = Convert.ToInt32(r["StammID"]);
                if (stammId <= 0) continue;

                if ((spalteDa && zielAnlage > 0)
                    ? FindePositionAnlage(zielProjektId, kategorieId,
                                          komponentenId, stammId, zielAnlage) > 0
                    : KostenPositionCtrl.FindePosition(zielProjektId, kategorieId,
                          komponentenId, stammId) > 0)
                {
                    e.Uebersprungen++;
                    continue;
                }

                int n = DataRepository.ExecuteNonQuery(
                    "INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, " +
                    "KategorieID, EingegebenerWert, BestCase, WorstCase, Nutzungsdauer, " +
                    "BestCase_Nutzungsdauer, WorstCase_Nutzungsdauer, Einheit, Gruppe, [" +
                    SchemaKatalog.SPALTE_PW_KOSTENART + "], [" +
                    SchemaKatalog.SPALTE_PW_BEMESSUNG + "], [" +
                    SchemaKatalog.SPALTE_PW_IST_ERLOES + "], [" +
                    SchemaKatalog.SPALTE_PW_MENGE + "], [" +
                    SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], [" +
                    SchemaKatalog.SPALTE_PW_VORLAGEID + "], [" +
                    SchemaKatalog.SPALTE_PW_STARTJAHR + "]) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    new DbParam("@p", zielProjektId),
                    new DbParam("@s", stammId),
                    new DbParam("@c", komponentenId),
                    new DbParam("@k", kategorieId),
                    Roh(r, "EingegebenerWert", DbParamTyp.Double),
                    Roh(r, "BestCase", DbParamTyp.Double),
                    Roh(r, "WorstCase", DbParamTyp.Double),
                    Roh(r, "Nutzungsdauer", DbParamTyp.Double),
                    Roh(r, "BestCase_Nutzungsdauer", DbParamTyp.Double),
                    Roh(r, "WorstCase_Nutzungsdauer", DbParamTyp.Double),
                    Roh(r, "Einheit", DbParamTyp.VarWChar),
                    Roh(r, "Gruppe", DbParamTyp.VarWChar),
                    Roh(r, SchemaKatalog.SPALTE_PW_KOSTENART, DbParamTyp.VarWChar),
                    Roh(r, SchemaKatalog.SPALTE_PW_BEMESSUNG, DbParamTyp.VarWChar),
                    Roh(r, SchemaKatalog.SPALTE_PW_IST_ERLOES, DbParamTyp.Boolean),
                    Roh(r, SchemaKatalog.SPALTE_PW_MENGE, DbParamTyp.Double),
                    Roh(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS, DbParamTyp.Double),
                    Roh(r, SchemaKatalog.SPALTE_PW_VORLAGEID, DbParamTyp.Integer),
                    Roh(r, SchemaKatalog.SPALTE_PW_STARTJAHR, DbParamTyp.Integer));

                if (n == 1)
                {
                    e.Angelegt++;
                    // Ä21: Der frisch kopierten Zeile die Ziel-Anlage samt
                    // Geräteanker geben (MAX(ID) = eben eingefügte Zeile).
                    if (spalteDa && zielAnlage > 0)
                    {
                        object neuId = DataRepository.ExecuteScalar(
                            "SELECT MAX(ID) FROM Tab_ProjektWerte WHERE ProjektID = ? AND " +
                            "KomponentenID = ? AND KategorieID = ? AND StammID = ?",
                            new DbParam("@p", zielProjektId),
                            new DbParam("@c", komponentenId),
                            new DbParam("@k", kategorieId),
                            new DbParam("@s", stammId));
                        if (neuId != null && neuId != DBNull.Value)
                            KostenProjektPositionenCtrl.AnlageZuordnen(
                                Convert.ToInt32(neuId), zielAnlage);
                    }
                }
                else
                {
                    e.Fehler = true;
                    e.Meldungen.Add("Zeile StammID " + stammId + " nicht kopierbar.");
                }
            }

            e.Meldungen.Add(e.Angelegt + " Positionen aus dem Quellprojekt kopiert, " +
                            e.Uebersprungen + " bereits vorhanden.");
            return e;
        }

        /// <summary>Ä21: Positionszahl je Anlage (idAnlage &gt; 0), ohne Zuordnung
        /// (0) oder gesamt (-1) — Grundlage der anlagenbezogenen Vorschau.</summary>
        public static int VorhandeneImProjekt(int projektId, int komponentenId,
                                              int kategorieId, int idAnlage)
        {
            bool spalteDa = false;
            try { spalteDa = KostenPositionCtrl.StelleSpaltenSicher(); } catch { }
            if (!spalteDa || idAnlage < 0)
                return VorhandeneImProjekt(projektId, komponentenId, kategorieId);

            string filter; var ps = new List<DbParam>
            {
                new DbParam("@p", projektId),
                new DbParam("@c", komponentenId),
                new DbParam("@k", kategorieId)
            };
            if (idAnlage > 0)
            {
                filter = " AND ID_Anlage = ?";
                ps.Add(new DbParam("@a", idAnlage));
            }
            else
            {
                // Projekt-Id als LITERAL — ACE-Unterabfragen-Falle (Ä21-Befund).
                filter = " AND (ID_Anlage IS NULL OR ID_Anlage NOT IN " +
                         "(SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = " +
                         projektId + "))";
            }
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ? AND " +
                "KomponentenID = ? AND KategorieID = ?" + filter, ps.ToArray());
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        // ----------------------------------------------------------------- intern ---

        /// <summary>
        /// StammID zur Bezeichnung — legt den Lexikoneintrag mit EXPLIZITER
        /// <c>MAX+1</c>-StammID an, wenn er fehlt (Muster Migrationsschritt 27).
        /// Bewusst NICHT <c>KostenPositionCtrl.StammIdNeben</c>: dessen INSERT ohne
        /// StammID schreibt eine 0 (dokumentierter Altbefund, SchemaKatalog).
        /// </summary>
        internal static int StammIdSicher(string bezeichnung)
        {
            if (string.IsNullOrWhiteSpace(bezeichnung)) return 0;

            object o = DataRepository.ExecuteScalar(
                "SELECT MAX(StammID) FROM Tab_Kostenfaktor WHERE Bezeichnung = ?",
                new DbParam("@b", bezeichnung));
            if (o != null && o != DBNull.Value && Convert.ToInt32(o) > 0)
                return Convert.ToInt32(o);

            object max = DataRepository.ExecuteScalar(
                "SELECT MAX(StammID) FROM Tab_Kostenfaktor");
            int neu = ((max == null || max == DBNull.Value) ? 0 : Convert.ToInt32(max)) + 1;

            int n = DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) " +
                "VALUES (?, ?, FALSE)",
                new DbParam("@s", neu),
                new DbParam("@b", bezeichnung));
            return n == 1 ? neu : 0;
        }

        /// <summary>Herkunftsvermerk (§ 4.2) und Nutzungsdauer-Vorbelegung (FK4/FK10:
        /// alle drei Szenariospalten erben den Vorlagenwert).</summary>
        private static void HerkunftUndNutzungsdauer(int positionsId, int vorlageId,
                                                     double? nutzungsdauer)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_VORLAGEID + "] = ? " +
                "WHERE ID = ?",
                new DbParam("@v", vorlageId),
                new DbParam("@id", positionsId));

            if (nutzungsdauer.HasValue)
                DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_ProjektWerte SET Nutzungsdauer = ?, " +
                    "BestCase_Nutzungsdauer = ?, WorstCase_Nutzungsdauer = ? WHERE ID = ?",
                    new DbParam("@n1", nutzungsdauer.Value),
                    new DbParam("@n2", nutzungsdauer.Value),
                    new DbParam("@n3", nutzungsdauer.Value),
                    new DbParam("@id", positionsId));
        }

        /// <summary>Feldwert 1:1 als typisierter Parameter (NULL bleibt NULL).</summary>
        private static DbParam Roh(DataRow r, string spalte, DbParamTyp typ)
        {
            var p = new DbParam("@" + spalte, typ);
            object w = r[spalte];
            p.Wert = (w == null || w == DBNull.Value) ? DBNull.Value : w;
            return p;
        }
    }
}
