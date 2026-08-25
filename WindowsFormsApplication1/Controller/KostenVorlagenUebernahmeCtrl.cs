using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

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
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@c", komponentenId),
                new OleDbParameter("@k", kategorieId));
            return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
        }

        // ------------------------------------------------- Vorlage -> Projekt ---

        /// <summary>
        /// Übernimmt eine Stammvorlage (Standard oder Variante) in ein Projekt.
        /// </summary>
        public static UebernahmeErgebnis AusVorlage(int projektId, KostenVorlageKopf vorlage)
        {
            var e = new UebernahmeErgebnis();
            if (vorlage == null || projektId <= 0)
            {
                e.Fehler = true;
                e.Meldungen.Add("Keine Vorlage oder kein Zielprojekt gewählt.");
                return e;
            }

            string gruppe = vorlage.KategorieId == Form_Kosten.KATEGORIE_BETRIEB
                ? DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI
                : DbWerte.KOSTEN_GRUPPE_ALLGEMEIN;

            foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(vorlage.Id))
            {
                int stammId = StammIdSicher(p.Bezeichnung);
                if (stammId <= 0)
                {
                    e.Fehler = true;
                    e.Meldungen.Add("Positionslexikon: \"" + p.Bezeichnung + "\" nicht anlegbar.");
                    continue;
                }

                // Vorhandene Zeile bleibt unberührt (NurAnlegen-Muster).
                if (KostenPositionCtrl.FindePosition(projektId, vorlage.KategorieId,
                        vorlage.KomponentenId, stammId) > 0)
                {
                    e.Uebersprungen++;
                    continue;
                }

                BemessungKatalog.Info info = BemessungKatalog.Finde(p.Bemessung);
                bool absolut = info != null && info.Absolut;
                double startBetrag = absolut && p.Satz.HasValue ? p.Satz.Value : 0.0;

                int id = KostenPositionCtrl.SetzeBetrag(projektId, vorlage.KategorieId,
                    vorlage.KomponentenId, stammId, startBetrag, gruppe, true);
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
                e.Angelegt++;
            }

            e.Meldungen.Add(e.Angelegt + " Positionen aus \"" + vorlage.Name +
                            "\" angelegt, " + e.Uebersprungen + " bereits vorhanden.");
            return e;
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
            var e = new UebernahmeErgebnis();
            if (zielProjektId <= 0 || quellProjektId <= 0 || zielProjektId == quellProjektId)
            {
                e.Fehler = true;
                e.Meldungen.Add("Quelle und Ziel müssen verschiedene Projekte sein.");
                return e;
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
                "FROM Tab_ProjektWerte WHERE ProjektID = ? AND KomponentenID = ? AND KategorieID = ?",
                new OleDbParameter("@p", quellProjektId),
                new OleDbParameter("@c", komponentenId),
                new OleDbParameter("@k", kategorieId));

            foreach (DataRow r in dt.Rows)
            {
                if (r["StammID"] == DBNull.Value) continue;
                int stammId = Convert.ToInt32(r["StammID"]);
                if (stammId <= 0) continue;

                if (KostenPositionCtrl.FindePosition(zielProjektId, kategorieId,
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
                    new OleDbParameter("@p", zielProjektId),
                    new OleDbParameter("@s", stammId),
                    new OleDbParameter("@c", komponentenId),
                    new OleDbParameter("@k", kategorieId),
                    Roh(r, "EingegebenerWert", OleDbType.Double),
                    Roh(r, "BestCase", OleDbType.Double),
                    Roh(r, "WorstCase", OleDbType.Double),
                    Roh(r, "Nutzungsdauer", OleDbType.Double),
                    Roh(r, "BestCase_Nutzungsdauer", OleDbType.Double),
                    Roh(r, "WorstCase_Nutzungsdauer", OleDbType.Double),
                    Roh(r, "Einheit", OleDbType.VarWChar),
                    Roh(r, "Gruppe", OleDbType.VarWChar),
                    Roh(r, SchemaKatalog.SPALTE_PW_KOSTENART, OleDbType.VarWChar),
                    Roh(r, SchemaKatalog.SPALTE_PW_BEMESSUNG, OleDbType.VarWChar),
                    Roh(r, SchemaKatalog.SPALTE_PW_IST_ERLOES, OleDbType.Boolean),
                    Roh(r, SchemaKatalog.SPALTE_PW_MENGE, OleDbType.Double),
                    Roh(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS, OleDbType.Double),
                    Roh(r, SchemaKatalog.SPALTE_PW_VORLAGEID, OleDbType.Integer),
                    Roh(r, SchemaKatalog.SPALTE_PW_STARTJAHR, OleDbType.Integer));

                if (n == 1) e.Angelegt++;
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
                new OleDbParameter("@b", bezeichnung));
            if (o != null && o != DBNull.Value && Convert.ToInt32(o) > 0)
                return Convert.ToInt32(o);

            object max = DataRepository.ExecuteScalar(
                "SELECT MAX(StammID) FROM Tab_Kostenfaktor");
            int neu = ((max == null || max == DBNull.Value) ? 0 : Convert.ToInt32(max)) + 1;

            int n = DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Kostenfaktor (StammID, Bezeichnung, IsMainComponent) " +
                "VALUES (?, ?, FALSE)",
                new OleDbParameter("@s", neu),
                new OleDbParameter("@b", bezeichnung));
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
                new OleDbParameter("@v", vorlageId),
                new OleDbParameter("@id", positionsId));

            if (nutzungsdauer.HasValue)
                DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_ProjektWerte SET Nutzungsdauer = ?, " +
                    "BestCase_Nutzungsdauer = ?, WorstCase_Nutzungsdauer = ? WHERE ID = ?",
                    new OleDbParameter("@n1", nutzungsdauer.Value),
                    new OleDbParameter("@n2", nutzungsdauer.Value),
                    new OleDbParameter("@n3", nutzungsdauer.Value),
                    new OleDbParameter("@id", positionsId));
        }

        /// <summary>Feldwert 1:1 als typisierter Parameter (NULL bleibt NULL).</summary>
        private static OleDbParameter Roh(DataRow r, string spalte, OleDbType typ)
        {
            var p = new OleDbParameter("@" + spalte, typ);
            object w = r[spalte];
            p.Value = (w == null || w == DBNull.Value) ? DBNull.Value : w;
            return p;
        }
    }
}
