using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Zuordnung Projekt ↔ Kältebedarf</b> (<c>Z_Projekt_Kaeltebedarf</c>, Welle K1; Muster
    /// <see cref="Z_ProjektProzesswaermeCtrl"/>): Lesen je Projekt, Jahressumme, Deckungsart. Geschrieben und gelöscht wird
    /// über <see cref="WizardCtrl.Add_Projekt_Kaelte"/> / <see cref="WizardCtrl.Del_Projekt_Kaelte"/>. Die Deckungsregeln
    /// (Konzept Kältebedarf 3.4) stehen einmal hier: <see cref="Deckungspruefung"/> und <see cref="Normalisieren"/>.
    /// Spaltentolerant: vor dem Schritt <see cref="KaeltebedarfSchema"/> liest jeder Weg leer.
    /// </summary>
    class Z_ProjektKaeltebedarfCtrl
    {
        /// <summary>Die Zuordnungstabelle.</summary>
        public const string TABLE = KaeltebedarfSchema.TAB_ZUORDNUNG;

        /// <summary>Die Zeilen eines Projekts in der Folge ihrer ID.</summary>
        public static List<Z_ProjektKaeltebedarfModel> LiesProjekt(int idProjekt)
        {
            var liste = new List<Z_ProjektKaeltebedarfModel>();
            if (idProjekt <= 0 || !KaeltebedarfSchema.TabellenVorhanden()) return liste;

            // KWN-0: die Art nur, wenn der Schritt KaelteArtSchema steht - vorher heisst jede Zeile Raum.
            bool mitArt = KaelteArtSchema.ProfilLesbar();
            string sql =
                "SELECT z.ID, z.ID_Kaeltebedarf, k.Bezeichner, z.Summe, z.ID_Betriebskalender, z.Deckung, z.Split_EER_Weg, " +
                "z.Split_EER_1, z.Split_Taussen_1, z.Split_EER_2, z.Split_Taussen_2, z.Kuehl_ID_Carrier, z.Kuehl_EigenerZaehler, " +
                (mitArt ? "z.Kaelte_Art, " : "") +
                "k.Vorlauf, k.Ruecklauf " +
                "FROM Z_Projekt_Kaeltebedarf z INNER JOIN Tab_Kaeltebedarf k ON z.ID_Kaeltebedarf = k.ID " +
                "WHERE z.ID_Projekt = ? ORDER BY z.ID";
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@id", idProjekt));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
            {
                liste.Add(new Z_ProjektKaeltebedarfModel
                {
                    ID_Z = Ganz(r["ID"]) ?? 0,
                    ID_Projekt = idProjekt,
                    ID_Kaeltebedarf = Ganz(r["ID_Kaeltebedarf"]) ?? 0,
                    Bezeichner = r["Bezeichner"] == DBNull.Value ? "" : r["Bezeichner"].ToString(),
                    Summe = Zahl(r["Summe"]) ?? 0.0,
                    ID_Betriebskalender = Ganz(r["ID_Betriebskalender"]),
                    Deckung = r["Deckung"] == DBNull.Value ? KaeltebedarfSchema.DECKUNG_ZENTRAL : r["Deckung"].ToString(),
                    EerWeg = r["Split_EER_Weg"] == DBNull.Value ? null : r["Split_EER_Weg"].ToString(),
                    Eer1 = Zahl(r["Split_EER_1"]),
                    Taussen1 = Zahl(r["Split_Taussen_1"]),
                    Eer2 = Zahl(r["Split_EER_2"]),
                    Taussen2 = Zahl(r["Split_Taussen_2"]),
                    KuehlIdCarrier = Ganz(r["Kuehl_ID_Carrier"]),
                    KuehlEigenerZaehler = (Ganz(r["Kuehl_EigenerZaehler"]) ?? 0) != 0,
                    KaelteArt = mitArt ? KaelteArtSchema.Normal(r[KaelteArtSchema.SPALTE]) : KaelteArtSchema.RAUM,
                    Vorlauf = Zahl(r["Vorlauf"]),
                    Ruecklauf = Zahl(r["Ruecklauf"]),
                });
            }
            return liste;
        }

        /// <summary>Schreibt die Jahressumme der Zeilen mit diesem Bezeichner und markiert das Projekt als geändert.</summary>
        public static bool UpdateSumme(double summe, string bezeichner, int idProjekt)
        {
            if (!KaeltebedarfSchema.TabellenVorhanden()) return false;
            bool ok = DataRepository.ExecuteSQL(
                "UPDATE Z_Projekt_Kaeltebedarf SET Summe = ? WHERE ID_Projekt = ? AND ID_Kaeltebedarf IN " +
                "(SELECT ID FROM Tab_Kaeltebedarf WHERE ID_Projekt = ? AND Bezeichner = ?)",
                new DbParam("@summe", summe), new DbParam("@p", idProjekt), new DbParam("@pk", idProjekt),
                new DbParam("@bez", bezeichner ?? (object)DBNull.Value));
            if (ok) MerkmalUebernahmeCtrl.MarkiereProjektGeaendert(idProjekt);
            return ok;
        }

        /// <summary>
        /// Prüft die Deckungsfelder einer Zeile (Konzept Kältebedarf 3.4); <c>null</c> = zulässig, sonst der Grund. Bei
        /// „zentral“ ist alles zulässig (die Split-Felder leert <see cref="Normalisieren"/>); bei „split“ braucht „fest“ einen
        /// EER > 0 samt Außentemperatur im Bereich, „linear“ dazu den zweiten Punkt mit T₂ ≠ T₁.
        /// </summary>
        public static string Deckungspruefung(Z_ProjektKaeltebedarfModel z)
        {
            if (z == null) return "keine Zeile";
            if (z.Deckung == KaeltebedarfSchema.DECKUNG_ZENTRAL) return null;
            if (z.Deckung != KaeltebedarfSchema.DECKUNG_SPLIT) return "unbekannte Deckungsart";
            if (z.EerWeg != KaeltebedarfSchema.EER_FEST && z.EerWeg != KaeltebedarfSchema.EER_LINEAR) return "EER-Weg fehlt";
            if (!(z.Eer1 > 0)) return "EER muss groesser 0 sein";
            if (!ImBereich(z.Taussen1)) return "Aussentemperatur ausserhalb -20 ... 50 Grad C";
            if (z.EerWeg == KaeltebedarfSchema.EER_FEST) return null;
            if (!(z.Eer2 > 0)) return "EER am Punkt 2 muss groesser 0 sein";
            if (!ImBereich(z.Taussen2)) return "Aussentemperatur am Punkt 2 ausserhalb -20 ... 50 Grad C";
            if (z.Taussen2 == z.Taussen1) return "Die Aussentemperaturen beider Punkte muessen verschieden sein";
            return null;
        }

        /// <summary>Leert bei „zentral“ die Split-Felder (NULL, nicht 0), bei „fest“ den zweiten Punkt.</summary>
        public static void Normalisieren(Z_ProjektKaeltebedarfModel z)
        {
            if (z == null) return;
            if (string.IsNullOrEmpty(z.Deckung)) z.Deckung = KaeltebedarfSchema.DECKUNG_ZENTRAL;
            z.KaelteArt = KaelteArtSchema.Normal(z.KaelteArt);
            if (z.Deckung == KaeltebedarfSchema.DECKUNG_ZENTRAL)
            {
                z.EerWeg = null; z.Eer1 = null; z.Taussen1 = null; z.Eer2 = null; z.Taussen2 = null;
                z.KuehlIdCarrier = null; z.KuehlEigenerZaehler = false;
            }
            else if (z.EerWeg == KaeltebedarfSchema.EER_FEST)
            {
                z.Eer2 = null; z.Taussen2 = null;
            }
        }

        // ---- Die Lastgangzeile (Z_ProjektWaermebedarf, Kanal Kuehlung) - dieselben Regeln ----------------------------

        /// <summary>Gehört die Lastgangzeile zum Kanal Kühlung? Nur dort ist die Deckungsart wirksam (Konzept 5).</summary>
        public static bool IstKuehlkanal(Z_ProjWaermebedarfModel w) =>
            w != null && Z_ProjektGebGanglinieCtrl.KanalOderHeizung(w.Kanal) == DbWerte.KANAL_KUEHLUNG;

        /// <summary>
        /// Prüft die Deckungsfelder einer Lastgangzeile — im Kanal Kühlung nach denselben Regeln wie die Profilzeile
        /// (<see cref="Deckungspruefung(Z_ProjektKaeltebedarfModel)"/>); jeder andere Kanal ist zulässig, er schreibt stets
        /// „zentral“ (<see cref="Normalisieren(Z_ProjWaermebedarfModel)"/>).
        /// </summary>
        public static string Deckungspruefung(Z_ProjWaermebedarfModel w)
        {
            if (w == null) return "keine Zeile";
            return IstKuehlkanal(w) ? Deckungspruefung(Als(w)) : null;
        }

        /// <summary>
        /// Leert die Split-Felder einer Lastgangzeile wie bei der Profilzeile; außerhalb des Kanals Kühlung steht die Zeile
        /// stets auf „zentral“ mit leeren Split-Feldern.
        /// </summary>
        public static void Normalisieren(Z_ProjWaermebedarfModel w)
        {
            if (w == null) return;
            if (!IstKuehlkanal(w)) w.Deckung = KaeltebedarfSchema.DECKUNG_ZENTRAL;
            Z_ProjektKaeltebedarfModel k = Als(w);
            Normalisieren(k);
            w.Deckung = k.Deckung; w.EerWeg = k.EerWeg; w.Eer1 = k.Eer1; w.Taussen1 = k.Taussen1;
            w.Eer2 = k.Eer2; w.Taussen2 = k.Taussen2; w.KuehlIdCarrier = k.KuehlIdCarrier;
            w.KuehlEigenerZaehler = k.KuehlEigenerZaehler;
            // KWN-0: Die Art wirkt nur im Kanal Kuehlung; jeder andere Kanal schreibt Raum.
            w.KaelteArt = IstKuehlkanal(w) ? k.KaelteArt : KaelteArtSchema.RAUM;
        }

        /// <summary>Tragen die Lastgangzuordnungen die Deckungsspalten (Schritt <see cref="KaeltebedarfSchema"/>)?</summary>
        public static bool LastgangDeckungLesbar() =>
            DataRepository.SpalteVorhanden(KaeltebedarfSchema.TAB_LASTGANG, KaeltebedarfSchema.SPALTE_DECKUNG);

        /// <summary>
        /// Die Werte der acht Deckungsspalten einer (normalisierten) Lastgangzeile in der Folge von
        /// <see cref="KaeltebedarfSchema.DECKUNGSSPALTEN"/> — für den Schreibweg <see cref="WizardCtrl.Add_WaermebedarfExtern"/>.
        /// </summary>
        public static DbParam[] DeckungParameter(Z_ProjWaermebedarfModel w) => new[]
        {
            new DbParam("@deck", w.Deckung ?? KaeltebedarfSchema.DECKUNG_ZENTRAL),
            new DbParam("@weg", (object)w.EerWeg ?? DBNull.Value),
            new DbParam("@e1", (object)w.Eer1 ?? DBNull.Value),
            new DbParam("@t1", (object)w.Taussen1 ?? DBNull.Value),
            new DbParam("@e2", (object)w.Eer2 ?? DBNull.Value),
            new DbParam("@t2", (object)w.Taussen2 ?? DBNull.Value),
            new DbParam("@car", (object)w.KuehlIdCarrier ?? DBNull.Value),
            new DbParam("@zae", w.KuehlEigenerZaehler ? 1 : 0),
        };

        /// <summary>
        /// Liest die Deckungsfelder einer Lastgangzeile spaltentolerant aus einer gelesenen Zeile (<c>SELECT *</c>); fehlt
        /// eine Spalte (Stand vor Schritt 213), bleibt der Wert der Vorgabe.
        /// </summary>
        public static void DeckungLesen(DataRow r, Z_ProjWaermebedarfModel w)
        {
            if (r == null || w == null) return;
            DataColumnCollection c = r.Table.Columns;
            object Feld(string spalte) => c.Contains(spalte) ? r[spalte] : null;
            object deckung = Feld(KaeltebedarfSchema.SPALTE_DECKUNG);
            w.Deckung = deckung == null || deckung == DBNull.Value || deckung.ToString().Trim().Length == 0
                        ? KaeltebedarfSchema.DECKUNG_ZENTRAL : deckung.ToString().Trim();
            object weg = Feld(KaeltebedarfSchema.SPALTE_EER_WEG);
            w.EerWeg = weg == null || weg == DBNull.Value ? null : weg.ToString();
            w.Eer1 = Zahl(Feld(KaeltebedarfSchema.SPALTE_EER_1));
            w.Taussen1 = Zahl(Feld(KaeltebedarfSchema.SPALTE_TAUSSEN_1));
            w.Eer2 = Zahl(Feld(KaeltebedarfSchema.SPALTE_EER_2));
            w.Taussen2 = Zahl(Feld(KaeltebedarfSchema.SPALTE_TAUSSEN_2));
            w.KuehlIdCarrier = Ganz(Feld(KaeltebedarfSchema.SPALTE_KUEHL_ID_CARRIER));
            w.KuehlEigenerZaehler = (Ganz(Feld(KaeltebedarfSchema.SPALTE_KUEHL_EIGENER_ZAEHLER)) ?? 0) != 0;
            w.KaelteArt = KaelteArtSchema.Normal(Feld(KaelteArtSchema.SPALTE));
        }

        /// <summary>Übernimmt die Deckungsfelder von <paramref name="quelle"/> nach <paramref name="ziel"/>.</summary>
        public static void DeckungUebernehmen(Z_ProjWaermebedarfModel quelle, Z_ProjWaermebedarfModel ziel)
        {
            if (quelle == null || ziel == null) return;
            ziel.Deckung = quelle.Deckung; ziel.EerWeg = quelle.EerWeg; ziel.Eer1 = quelle.Eer1; ziel.Taussen1 = quelle.Taussen1;
            ziel.Eer2 = quelle.Eer2; ziel.Taussen2 = quelle.Taussen2; ziel.KuehlIdCarrier = quelle.KuehlIdCarrier;
            ziel.KuehlEigenerZaehler = quelle.KuehlEigenerZaehler;
            ziel.KaelteArt = quelle.KaelteArt;
        }

        private static Z_ProjektKaeltebedarfModel Als(Z_ProjWaermebedarfModel w) => new Z_ProjektKaeltebedarfModel
        {
            Bezeichner = w.m_szBezeichner ?? "",
            Deckung = w.Deckung, EerWeg = w.EerWeg, Eer1 = w.Eer1, Taussen1 = w.Taussen1, Eer2 = w.Eer2,
            Taussen2 = w.Taussen2, KuehlIdCarrier = w.KuehlIdCarrier, KuehlEigenerZaehler = w.KuehlEigenerZaehler,
            KaelteArt = w.KaelteArt,
        };

        private static bool ImBereich(double? t) =>
            t != null && t >= KaeltebedarfSchema.TAUSSEN_MIN && t <= KaeltebedarfSchema.TAUSSEN_MAX;

        private static double? Zahl(object o) => o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        private static int? Ganz(object o) => o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Der öffentliche Zugang der Oberfläche zur Deckungsregel (Konzept Kältebedarf 3.4, K5a): die Feldgruppe „Deckung“
    /// (<c>EPOS.UI</c>, <c>DeckungsartDaten</c>) prüft mit DERSELBEN Regel wie der Schreibweg und das Warnkriterium
    /// <c>DEZENTRALE_KAELTE_OHNE_EER</c> — <see cref="Z_ProjektKaeltebedarfCtrl.Deckungspruefung(Z_ProjektKaeltebedarfModel)"/>.
    /// </summary>
    public static class Kaeltedeckungsregel
    {
        /// <summary><c>null</c> = zulässig, sonst der Prüfgrund des Kerns.</summary>
        public static string Pruefe(Z_ProjektKaeltebedarfModel zeile) => Z_ProjektKaeltebedarfCtrl.Deckungspruefung(zeile);
    }
}
