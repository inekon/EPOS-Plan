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

            const string sql =
                "SELECT z.ID, z.ID_Kaeltebedarf, k.Bezeichner, z.Summe, z.ID_Betriebskalender, z.Deckung, z.Split_EER_Weg, " +
                "z.Split_EER_1, z.Split_Taussen_1, z.Split_EER_2, z.Split_Taussen_2, z.Kuehl_ID_Carrier, z.Kuehl_EigenerZaehler, " +
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

        private static bool ImBereich(double? t) =>
            t != null && t >= KaeltebedarfSchema.TAUSSEN_MIN && t <= KaeltebedarfSchema.TAUSSEN_MAX;

        private static double? Zahl(object o) => o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        private static int? Ganz(object o) => o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);
    }
}
