using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Anlagenzeile der Kältemaschine (<c>Tab_Energieanlagen</c> mit Typ
    /// <see cref="KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE"/>, Schemaschritt 183) samt den Kühleingaben
    /// ihrer Projektkopie.
    /// </summary>
    public sealed class KaeltemaschineAnlageModel
    {
        /// <summary><c>Tab_Energieanlagen.ID</c>.</summary>
        public int AnlagenId { get; set; }

        /// <summary><c>Tab_Energieanlagen.ID_Projekt</c>.</summary>
        public int IdProjekt { get; set; }

        /// <summary>Anzeigename der Anlage.</summary>
        public string Bezeichner { get; set; } = "";

        /// <summary>Verweis auf die Projektkopie; <c>null</c> = Gerät gelöscht.</summary>
        public int? IdKaeltemaschine { get; set; }

        /// <summary>Anzahl gleicher Maschinen (mindestens 1).</summary>
        public int Anzahl { get; set; } = 1;

        /// <summary>Kühlträger (<c>Kuehl_ID_Carrier</c>); <c>null</c> = Stromträger des Projekts.</summary>
        public int? KuehlIdCarrier { get; set; }

        /// <summary>Abrechnung über einen eigenen Zähler (<c>Kuehl_EigenerZaehler</c>); <c>null</c> = anteilig.</summary>
        public bool? KuehlEigenerZaehler { get; set; }

        /// <summary>Kaltwasservorlauf der Projektkopie [°C]; <c>null</c> = kleinste Stützstelle der Kennlinie.</summary>
        public double? KuehlVorlauf { get; set; }

        /// <summary>Hilfsstromanteil der Projektkopie [0…1); <c>null</c> = kein Zuschlag.</summary>
        public double? KuehlHilfsstromanteil { get; set; }
    }

    /// <summary>
    /// <b>Die Kältemaschine als Anlage</b> (KU3-4; Kühlkonzept 5.3, 5.5, 7.3): Lesen, Anlegen, Speichern und
    /// Löschen der Anlagenzeilen. Die Projektkopie entsteht über
    /// <see cref="KaeltemaschineCtrl.AusKatalogUebernehmen"/>; ihre Kühleingaben stehen an der Kopie, Anzahl,
    /// Kühlträger und Abrechnungsart an der Anlagenzeile.
    /// </summary>
    public static class KaeltemaschineAnlageCtrl
    {
        private const string SQL_LISTE =
            "SELECT a.ID, a.ID_Projekt, a.Bezeichner, a.ID_Kaeltemaschine, a.Kaeltemaschine_Anzahl, a.Kuehl_ID_Carrier, " +
            "a.Kuehl_EigenerZaehler, k.Kuehl_Vorlauf, k.Kuehl_Hilfsstromanteil " +
            "FROM Tab_Energieanlagen a LEFT JOIN Tab_Kaeltemaschine k ON k.ID = a.ID_Kaeltemaschine ";

        /// <summary>Die Anlagenzeilen der Kältemaschine eines Projekts in der Reihenfolge ihrer Kennung.</summary>
        public static IReadOnlyList<KaeltemaschineAnlageModel> Liste(int projektId)
        {
            var liste = new List<KaeltemaschineAnlageModel>();
            DataTable dt = DataRepository.GetDataTable(SQL_LISTE + "WHERE a.ID_Projekt = ? AND a.ID_Type = ? ORDER BY a.ID",
                new DbParam("?", projektId), new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE));
            if (dt != null) foreach (DataRow r in dt.Rows) liste.Add(AusZeile(r));
            return liste;
        }

        /// <summary>
        /// Die Anlagenzeilen für den Rechenweg: still — eine Datenbank vor Schritt
        /// <see cref="KaeltemaschineAnlageSchema.SCHRITT"/> oder ein Lesefehler liefert die leere Liste.
        /// </summary>
        public static IReadOnlyList<KaeltemaschineAnlageModel> ListeStill(int projektId)
        {
            try
            {
                if (!DataRepository.SpalteVorhanden(KaeltemaschineAnlageSchema.TAB_ANLAGEN, KaeltemaschineAnlageSchema.SPALTE_ID_KAELTEMASCHINE))
                    return Array.Empty<KaeltemaschineAnlageModel>();
                return Liste(projektId);
            }
            catch (Exception)
            {
                return Array.Empty<KaeltemaschineAnlageModel>();
            }
        }

        /// <summary>Eine Anlagenzeile; <c>null</c>, wenn es sie nicht gibt oder sie keine Kältemaschine ist.</summary>
        public static KaeltemaschineAnlageModel Laden(int anlagenId)
        {
            DataTable dt = DataRepository.GetDataTable(SQL_LISTE + "WHERE a.ID = ? AND a.ID_Type = ?",
                new DbParam("?", anlagenId), new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE));
            return dt == null || dt.Rows.Count == 0 ? null : AusZeile(dt.Rows[0]);
        }

        /// <summary>
        /// Legt eine Anlage an: Projektkopie des Katalogsatzes <paramref name="stammId"/> (eine vorhandene Kopie
        /// desselben Satzes wird genommen) und die Anlagenzeile mit Typ 13 und Anzahl 1. Liefert die Anlagen-ID,
        /// <c>-1</c>, wenn es den Katalogsatz nicht gibt.
        /// </summary>
        public static int Anlegen(int projektId, int stammId, string bezeichner)
        {
            int kopie = KaeltemaschineCtrl.AusKatalogUebernehmen(stammId, projektId);
            if (kopie <= 0) return -1;
            if (string.IsNullOrWhiteSpace(bezeichner))
                bezeichner = KaeltemaschineCtrl.Laden(kopie)?.Bezeichner ?? DbWerte.ERZEUGER_KAELTEMASCHINE;
            DataRepository.ExecuteNonQuery(
                "INSERT INTO Tab_Energieanlagen (ID_Projekt, Bezeichner, ID_Type, ID_Kaeltemaschine, Kaeltemaschine_Anzahl) " +
                "VALUES (?, ?, ?, ?, 1)",
                new DbParam("?", projektId), new DbParam("?", bezeichner),
                new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE), new DbParam("?", kopie));
            object id = DataRepository.ExecuteScalar(
                "SELECT MAX(ID) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Kaeltemaschine = ?",
                new DbParam("?", projektId), new DbParam("?", kopie));
            return id == null || id == DBNull.Value ? -1 : Convert.ToInt32(id, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Prüft die Eingaben einer Anlage; <c>null</c> = in Ordnung, sonst der Meldungstext.
        /// </summary>
        public static string Pruefen(KaeltemaschineAnlageModel a)
        {
            if (a == null) return MyResource.Resource.KM_ANLAGE_FEHLT;
            if (string.IsNullOrWhiteSpace(a.Bezeichner)) return MyResource.Resource.KM_ANLAGE_NAME_LEER;
            if (a.Anzahl < 1) return MyResource.Resource.KM_ANLAGE_ANZAHL_UNGUELTIG;
            if (a.KuehlHilfsstromanteil.HasValue && !(a.KuehlHilfsstromanteil.Value >= 0 && a.KuehlHilfsstromanteil.Value < 1))
                return MyResource.Resource.KM_ANLAGE_HILFSSTROM_UNGUELTIG;
            if (a.KuehlVorlauf.HasValue && !(a.KuehlVorlauf.Value >= -20 && a.KuehlVorlauf.Value <= 30))
                return MyResource.Resource.KM_ANLAGE_VORLAUF_UNGUELTIG;
            return null;
        }

        /// <summary>Schreibt Anlagenzeile und Kühleingaben der Projektkopie in einem Vorgang; liefert den Prüftext oder <c>null</c>.</summary>
        public static string Speichern(KaeltemaschineAnlageModel a)
        {
            string fehler = Pruefen(a);
            if (fehler != null) return fehler;
            bool abweichend = a.KuehlIdCarrier.HasValue && a.KuehlIdCarrier.Value > 0;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren(
                        "UPDATE Tab_Energieanlagen SET Bezeichner = ?, Kaeltemaschine_Anzahl = ?, Kuehl_ID_Carrier = ?, " +
                        "Kuehl_EigenerZaehler = ? WHERE ID = ? AND ID_Type = ?",
                        new DbParam("?", a.Bezeichner.Trim()), new DbParam("?", a.Anzahl),
                        new DbParam("?", abweichend ? (object)a.KuehlIdCarrier.Value : DBNull.Value),
                        new DbParam("?", abweichend && a.KuehlEigenerZaehler == true ? (object)1 : DBNull.Value),
                        new DbParam("?", a.AnlagenId), new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE));
                    if (a.IdKaeltemaschine.HasValue)
                        v.Ausfuehren("UPDATE Tab_Kaeltemaschine SET Kuehl_Vorlauf = ?, Kuehl_Hilfsstromanteil = ? WHERE ID = ?",
                            new DbParam("?", (object)a.KuehlVorlauf ?? DBNull.Value),
                            new DbParam("?", (object)a.KuehlHilfsstromanteil ?? DBNull.Value),
                            new DbParam("?", a.IdKaeltemaschine.Value));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
            return null;
        }

        /// <summary>
        /// Löscht die Anlagenzeile — und ihre Projektkopie, wenn keine andere Anlagenzeile sie mehr führt.
        /// </summary>
        public static void Loeschen(int anlagenId)
        {
            KaeltemaschineAnlageModel a = Laden(anlagenId);
            if (a == null) return;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("DELETE FROM Tab_Energieanlagen WHERE ID = ? AND ID_Type = ?",
                        new DbParam("?", anlagenId), new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE));
                    if (a.IdKaeltemaschine.HasValue)
                        v.Ausfuehren("DELETE FROM Tab_Kaeltemaschine WHERE ID = ? AND NOT EXISTS " +
                                     "(SELECT 1 FROM Tab_Energieanlagen WHERE ID_Kaeltemaschine = ?)",
                            new DbParam("?", a.IdKaeltemaschine.Value), new DbParam("?", a.IdKaeltemaschine.Value));
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                    throw;
                }
            }
        }

        private static KaeltemaschineAnlageModel AusZeile(DataRow r)
        {
            double? Z(string s) => KaeltemaschineStammCtrl.Zahl(r[s]);
            double? carrier = Z("Kuehl_ID_Carrier");
            double? zaehler = Z("Kuehl_EigenerZaehler");
            double? km = Z("ID_Kaeltemaschine");
            return new KaeltemaschineAnlageModel
            {
                AnlagenId = KaeltemaschineStammCtrl.Ganz(r["ID"]),
                IdProjekt = KaeltemaschineStammCtrl.Ganz(r["ID_Projekt"]),
                Bezeichner = KaeltemaschineStammCtrl.Text(r["Bezeichner"]) ?? "",
                IdKaeltemaschine = km.HasValue ? (int?)(int)km.Value : null,
                Anzahl = Math.Max(1, KaeltemaschineStammCtrl.Ganz(r["Kaeltemaschine_Anzahl"])),
                KuehlIdCarrier = carrier.HasValue && carrier.Value > 0 ? (int?)(int)carrier.Value : null,
                KuehlEigenerZaehler = zaehler.HasValue ? (bool?)(zaehler.Value != 0) : null,
                KuehlVorlauf = Z("Kuehl_Vorlauf"),
                KuehlHilfsstromanteil = Z("Kuehl_Hilfsstromanteil"),
            };
        }
    }
}
