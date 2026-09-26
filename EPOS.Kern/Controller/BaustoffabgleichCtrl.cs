using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Datenbankseite des Namensabgleichs der Baustoffe</b> (Mehrzonenkonzept 3.5 und 6.3): liest
    /// Katalog, Synonyme der Auslieferung und die gemerkten Zuordnungen eines Projekts
    /// (<see cref="IBaustoffabgleichQuelle"/>) und merkt bzw. vergisst eine Zuordnung des Anwenders (N7).
    /// Nur über <see cref="DataRepository"/> mit <c>?</c>-Parametern.
    ///
    /// <para><b>Je Projekt</b> (Befund P, § 3.6; E27 zu M9): Eine Zuordnung gilt im Projekt, in dem sie
    /// getroffen wurde — sie reist mit Projektkopie und Transfer und fällt mit dem Projekt. Die
    /// Auslieferung kennt allein die Synonyme.</para>
    ///
    /// <para><b>Ohne die Tabellen</b> (eine Datenbank vor dem Schritt <see cref="BaustoffabgleichSchema.SCHRITT"/>)
    /// liefern die Leser leere Listen; der Abgleich trifft dann nur über N3, N5 und N6.</para>
    /// </summary>
    internal sealed class BaustoffabgleichCtrl : IBaustoffabgleichQuelle
    {
        private readonly int? _idProjekt;

        /// <summary>Das Ergebnis einer Schreibaktion — Erfolg, Meldung in der Anzeigekultur, Id.</summary>
        internal sealed record Ergebnis(bool Ok, string Meldung, int Id)
        {
            internal static Ergebnis Gut(int id) => new Ergebnis(true, "", id);
            internal static Ergebnis Fehler(string meldung) => new Ergebnis(false, meldung ?? "", 0);
        }

        /// <summary>Die Quelle für ein Projekt; <c>null</c> = ohne gemerkte Zuordnungen.</summary>
        internal BaustoffabgleichCtrl(int? idProjekt = null)
        {
            _idProjekt = idProjekt > 0 ? idProjekt : null;
        }

        /// <summary>Der Abgleich über diese Quelle — liest die drei Listen einmal.</summary>
        internal Baustoffabgleich Abgleich() => new Baustoffabgleich(this);

        // =================================================================
        //  Lesen (IBaustoffabgleichQuelle)
        // =================================================================

        public IReadOnlyList<BaustoffModel> Baustoffe()
        {
            if (!DataRepository.TabelleVorhanden(BaustoffSchema.TAB_STAMM)) return Array.Empty<BaustoffModel>();
            DataTable t = DataRepository.GetDataTable("SELECT * FROM \"" + BaustoffSchema.TAB_STAMM + "\" ORDER BY \"ID\"");
            return t == null ? Array.Empty<BaustoffModel>() : t.Rows.Cast<DataRow>().Select(BaustoffCtrl.AusZeile).ToList();
        }

        public IReadOnlyList<BaustoffSynonym> Synonyme()
        {
            if (!DataRepository.TabelleVorhanden(BaustoffabgleichSchema.TAB_SYNONYM)) return Array.Empty<BaustoffSynonym>();
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Materialname\", \"Sprache\", \"ID_Baustoff\", \"Quelle\" FROM \"" + BaustoffabgleichSchema.TAB_SYNONYM +
                "\" ORDER BY \"ID\"");
            if (t == null) return Array.Empty<BaustoffSynonym>();
            return t.Rows.Cast<DataRow>()
                    .Select(r => new BaustoffSynonym(BaustoffCtrl.TextAus(r, "Materialname") ?? "", BaustoffCtrl.TextAus(r, "Sprache"),
                                                     BaustoffCtrl.GanzAus(r, "ID_Baustoff") ?? 0, BaustoffCtrl.TextAus(r, "Quelle")))
                    .ToList();
        }

        public IReadOnlyList<BaustoffNamenzuordnung> Anwenderzuordnungen()
        {
            if (!_idProjekt.HasValue || !DataRepository.TabelleVorhanden(BaustoffabgleichSchema.TAB_ZUORDNUNG))
                return Array.Empty<BaustoffNamenzuordnung>();
            return LesenJeProjekt(_idProjekt.Value);
        }

        /// <summary>Die gemerkten Zuordnungen eines Projekts, nach Materialname.</summary>
        internal static IReadOnlyList<BaustoffNamenzuordnung> LesenJeProjekt(int idProjekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT \"Materialname\", \"ID_Baustoff\", \"Zeitpunkt\" FROM \"" + BaustoffabgleichSchema.TAB_ZUORDNUNG +
                "\" WHERE \"ID_Projekt\" = ? ORDER BY \"Materialname\"", new DbParam("@p", idProjekt));
            if (t == null) return Array.Empty<BaustoffNamenzuordnung>();
            return t.Rows.Cast<DataRow>()
                    .Select(r => new BaustoffNamenzuordnung(BaustoffCtrl.TextAus(r, "Materialname") ?? "",
                                                            BaustoffCtrl.GanzAus(r, "ID_Baustoff") ?? 0, BaustoffCtrl.TextAus(r, "Zeitpunkt")))
                    .ToList();
        }

        // =================================================================
        //  Merken und Vergessen (N7)
        // =================================================================

        /// <summary>
        /// <b>Merkt eine Zuordnung</b> Materialname → Katalogbaustoff für ein Projekt — unter dem
        /// normalisierten Namen (<see cref="Baustoffabgleich.Schluessel"/>); eine vorhandene Zuordnung
        /// desselben Namens wird ersetzt (EIN Vorgang). Benannt abgelehnt: ein leerer Name, ein
        /// Katalogbaustoff, den es nicht gibt, ein Projekt, das es nicht gibt.
        /// </summary>
        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="materialname">Der Materialname der Datei (roh oder normalisiert).</param>
        /// <param name="idBaustoff">Der Katalogbaustoff (<c>Tab_Baustoff_STAMM.ID</c>).</param>
        /// <param name="zeitpunkt">Der Zeitpunkt (ISO 8601); <c>null</c> = jetzt (UTC).</param>
        internal static Ergebnis Merken(int idProjekt, string materialname, int idBaustoff, string zeitpunkt = null)
        {
            string name = Baustoffabgleich.Schluessel(materialname);
            if (name.Length == 0) return Ergebnis.Fehler(MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_NAME);
            string wann = string.IsNullOrWhiteSpace(zeitpunkt)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
                : zeitpunkt.Trim();
            if (wann.Length > BaustoffabgleichSchema.LAENGE_ZEITPUNKT) wann = wann.Substring(0, BaustoffabgleichSchema.LAENGE_ZEITPUNKT);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    if (Convert.ToInt64(v.Skalar("SELECT COUNT(*) FROM \"" + BaustoffSchema.TAB_STAMM + "\" WHERE \"ID\" = ?",
                                                 new DbParam("@b", idBaustoff)), CultureInfo.InvariantCulture) == 0)
                    {
                        v.Rollback();
                        return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_STOFF, idBaustoff));
                    }
                    v.Ausfuehren("DELETE FROM \"" + BaustoffabgleichSchema.TAB_ZUORDNUNG + "\" WHERE \"ID_Projekt\" = ? AND \"Materialname\" = ?",
                                 new DbParam("@p", idProjekt), new DbParam("@m", name));
                    int id = v.EinfuegenUndId(
                        "INSERT INTO \"" + BaustoffabgleichSchema.TAB_ZUORDNUNG + "\" (\"ID_Projekt\", \"Materialname\", \"ID_Baustoff\", \"Zeitpunkt\") " +
                        "VALUES (?, ?, ?, ?)",
                        new[] { new DbParam("@p", idProjekt), new DbParam("@m", name), new DbParam("@b", idBaustoff), new DbParam("@z", wann) });
                    v.Commit();
                    return Ergebnis.Gut(id);
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_FEHLER, ex.Message));
            }
        }

        /// <summary>Vergisst die Zuordnung eines Materialnamens im Projekt; wahr, wenn eine bestand.</summary>
        internal static bool Vergessen(int idProjekt, string materialname)
        {
            string name = Baustoffabgleich.Schluessel(materialname);
            if (name.Length == 0) return false;
            return DataRepository.ExecuteNonQuery(
                "DELETE FROM \"" + BaustoffabgleichSchema.TAB_ZUORDNUNG + "\" WHERE \"ID_Projekt\" = ? AND \"Materialname\" = ?",
                new DbParam("@p", idProjekt), new DbParam("@m", name)) > 0;
        }
    }
}
