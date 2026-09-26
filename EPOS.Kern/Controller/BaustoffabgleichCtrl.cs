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

        /// <summary>Eine gemerkte Zuordnung samt ihrem Katalogbaustoff — eine Zeile der Ansicht je Projekt.</summary>
        /// <param name="Materialname">Der normalisierte Materialname (<see cref="Baustoffabgleich.Schluessel"/>).</param>
        /// <param name="IdBaustoff">Der Katalogbaustoff (<c>Tab_Baustoff_STAMM.ID</c>).</param>
        /// <param name="Zeitpunkt">Der Zeitpunkt der Zuordnung (ISO 8601).</param>
        /// <param name="Baustoff">Der Katalogbaustoff; <c>null</c>, wenn der Katalog ihn nicht mehr führt.</param>
        internal sealed record GemerkteZuordnung(string Materialname, int IdBaustoff, string Zeitpunkt, BaustoffModel Baustoff);

        /// <summary>
        /// <b>Die gemerkten Zuordnungen eines Projekts samt Katalogbaustoff</b>, nach Materialname — für die
        /// Ansicht „Baustoff-Zuordnungen…" im Gebäudedialog. Zwei Lesungen: die Zuordnungen und die
        /// Katalogzeilen, auf die sie zeigen. Ohne die Tabelle (Datenbank vor dem Schritt
        /// <see cref="BaustoffabgleichSchema.SCHRITT"/>) eine leere Liste.
        /// </summary>
        internal static IReadOnlyList<GemerkteZuordnung> GemerkteJeProjekt(int idProjekt)
        {
            if (!DataRepository.TabelleVorhanden(BaustoffabgleichSchema.TAB_ZUORDNUNG)) return Array.Empty<GemerkteZuordnung>();
            IReadOnlyList<BaustoffNamenzuordnung> zuordnungen = LesenJeProjekt(idProjekt);
            if (zuordnungen.Count == 0) return Array.Empty<GemerkteZuordnung>();

            var stoffe = new Dictionary<int, BaustoffModel>();
            if (DataRepository.TabelleVorhanden(BaustoffSchema.TAB_STAMM))
            {
                DataTable t = DataRepository.GetDataTable(
                    "SELECT * FROM \"" + BaustoffSchema.TAB_STAMM + "\" WHERE \"ID\" IN (SELECT \"ID_Baustoff\" FROM \"" +
                    BaustoffabgleichSchema.TAB_ZUORDNUNG + "\" WHERE \"ID_Projekt\" = ?)", new DbParam("@p", idProjekt));
                if (t != null)
                    foreach (BaustoffModel b in t.Rows.Cast<DataRow>().Select(BaustoffCtrl.AusZeile))
                        stoffe[b.ID] = b;
            }
            return zuordnungen
                   .Select(z => new GemerkteZuordnung(z.Materialname, z.IdBaustoff, z.Zeitpunkt,
                                                      stoffe.TryGetValue(z.IdBaustoff, out BaustoffModel b) ? b : null))
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
            string wann = Zeitpunkt(zeitpunkt);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    Ergebnis e = MerkenIn(v, idProjekt, name, idBaustoff, wann);
                    if (e.Ok) v.Commit();
                    else v.Rollback();
                    return e;
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_FEHLER, ex.Message));
            }
        }

        /// <summary>
        /// <b>Schreibt die Zuordnungen eines Dialogs</b> für ein Projekt — in EINEM Vorgang: Schlüssel →
        /// Id merkt (<see cref="Merken"/>), Schlüssel → <c>null</c> vergisst (<see cref="Vergessen"/>).
        /// Scheitert eine, wird keine geschrieben (die Meldung nennt den Grund). Mit
        /// <paramref name="vorgang"/> (oder in der <see cref="Vorgangsklammer"/> eines Aufrufers) läuft
        /// das Schreiben als Sicherungspunkt in dessen Vorgang — der Schreibweg der Gebäudeliste
        /// (<c>WizardCtrl.GebaeudeZuordnungAnlegen</c>) schreibt so Projektkopie, Zuordnungen,
        /// Bauteilvorschlag und Herkunft zusammen oder gar nicht.
        /// </summary>
        /// <returns>Erfolg mit der Zahl der geschriebenen Zuordnungen (gemerkt und vergessen) als Id.</returns>
        internal static Ergebnis Schreiben(int idProjekt, IReadOnlyDictionary<string, int?> zuordnungen, DbVorgang vorgang = null)
        {
            if (zuordnungen == null || zuordnungen.Count == 0) return Ergebnis.Gut(0);
            string wann = Zeitpunkt(null);
            using Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    int zahl = 0;
                    foreach (KeyValuePair<string, int?> paar in zuordnungen.OrderBy(p => p.Key, StringComparer.Ordinal))
                    {
                        string name = Baustoffabgleich.Schluessel(paar.Key);
                        if (name.Length == 0)
                        {
                            v.Rollback();
                            return Ergebnis.Fehler(MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_NAME);
                        }
                        if (paar.Value is int idBaustoff)
                        {
                            Ergebnis e = MerkenIn(v, idProjekt, name, idBaustoff, wann);
                            if (!e.Ok)
                            {
                                v.Rollback();
                                return e;
                            }
                        }
                        else
                        {
                            v.Ausfuehren("DELETE FROM \"" + BaustoffabgleichSchema.TAB_ZUORDNUNG + "\" WHERE \"ID_Projekt\" = ? AND \"Materialname\" = ?",
                                         new DbParam("@p", idProjekt), new DbParam("@m", name));
                        }
                        zahl++;
                    }
                    v.Commit();
                    return Ergebnis.Gut(zahl);
                }
            }
            catch (Exception ex)
            {
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_FEHLER, ex.Message));
            }
        }

        /// <summary>Merkt EINE Zuordnung im Vorgang <paramref name="v"/> — der Name ist schon normalisiert.</summary>
        private static Ergebnis MerkenIn(DbVorgang v, int idProjekt, string name, int idBaustoff, string wann)
        {
            if (Convert.ToInt64(v.Skalar("SELECT COUNT(*) FROM \"" + BaustoffSchema.TAB_STAMM + "\" WHERE \"ID\" = ?",
                                         new DbParam("@b", idBaustoff)), CultureInfo.InvariantCulture) == 0)
                return Ergebnis.Fehler(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BAUSTOFF_MSG_ZUORDNUNG_STOFF, idBaustoff));
            v.Ausfuehren("DELETE FROM \"" + BaustoffabgleichSchema.TAB_ZUORDNUNG + "\" WHERE \"ID_Projekt\" = ? AND \"Materialname\" = ?",
                         new DbParam("@p", idProjekt), new DbParam("@m", name));
            int id = v.EinfuegenUndId(
                "INSERT INTO \"" + BaustoffabgleichSchema.TAB_ZUORDNUNG + "\" (\"ID_Projekt\", \"Materialname\", \"ID_Baustoff\", \"Zeitpunkt\") " +
                "VALUES (?, ?, ?, ?)",
                new[] { new DbParam("@p", idProjekt), new DbParam("@m", name), new DbParam("@b", idBaustoff), new DbParam("@z", wann) });
            return Ergebnis.Gut(id);
        }

        /// <summary>Der Zeitpunkt einer Zuordnung (ISO 8601); leer = jetzt (UTC), gekürzt auf die Spaltenlänge.</summary>
        private static string Zeitpunkt(string zeitpunkt)
        {
            string wann = string.IsNullOrWhiteSpace(zeitpunkt)
                ? DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)
                : zeitpunkt.Trim();
            return wann.Length > BaustoffabgleichSchema.LAENGE_ZEITPUNKT ? wann.Substring(0, BaustoffabgleichSchema.LAENGE_ZEITPUNKT) : wann;
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
