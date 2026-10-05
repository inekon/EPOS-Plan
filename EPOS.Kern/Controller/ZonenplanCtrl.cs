using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Datenbankweg des Zonenplans</b> (Mehrzonenkonzept 6.4): die Nutzung einer gespeicherten Zone als
    /// Kalenderkopien der ausgelieferten Vorlagen (<see cref="NutzungUebernehmen"/>) und der gespeicherte Plan eines
    /// früheren Imports derselben Datei (<see cref="Gespeichert"/>). Ohne Schemaschritt: Name in <c>Tab_Zone.Bezeichner</c>,
    /// Nutzung in <c>Tab_Konditionierungskalender.Nutzung</c> der Kopien, Raum → Zone in <c>Tab_Importzuordnung</c>, die
    /// Regel in <c>Tab_Importquelle.Zonenregel</c>. Nur über <see cref="DataRepository"/> mit <c>?</c>-Parametern.
    /// </summary>
    internal static class ZonenplanCtrl
    {
        /// <summary>
        /// <b>Gibt einer Zone die Vorlagen ihrer Nutzung</b>: je Größe die erste ausgelieferte Vorlage dieser Nutzung in der
        /// Ordnung der Auswahlliste, übernommen über den Vorlagenweg (<see cref="KonditionierungsvorlageCtrl.Uebernehmen"/>:
        /// Kalenderkopie mit den Ferien der Zone, Nutzung an der Kopie) auf die wirksame Matrix der Zone. Eine Größe ohne
        /// Vorlage dieser Nutzung bleibt ohne Kalender. Läuft im Vorgang des Aufrufers, wenn einer angemeldet ist
        /// (<see cref="Vorgangsklammer"/>; je Größe ein Sicherungspunkt).
        /// </summary>
        /// <returns><c>null</c>, wenn alles übernommen ist; sonst die erste Ablehnung.</returns>
        internal static string NutzungUebernehmen(int idGebaeude, int idZone, string nutzung)
        {
            if (nutzung == null) return null;
            if (!Zonenplan.NUTZUNGEN.Contains(nutzung))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_IFC_PROT_PLAN_NUTZUNG_UNGUELTIG, nutzung);
            var vorlagen = new KonditionierungsvorlageCtrl();
            var kond = new KonditionierungCtrl();
            KonditionierungCtrl.Eigner ziel = KonditionierungCtrl.Eigner.Zone(idGebaeude, idZone);
            foreach (Konditionierungsgroesse groesse in Konditionierungsgroessen.Alle)
            {
                KonditionierungsvorlageCtrl.Vorlage vorlage = vorlagen.Liste(groesse)
                    .FirstOrDefault(x => x.Ausgeliefert && string.Equals(x.Nutzung, nutzung, StringComparison.Ordinal));
                if (vorlage == null) continue;
                Konditionierungsarbeitsstand stand = kond.ArbeitsstandLesen(idGebaeude, null, out string meldung);
                if (stand == null) return meldung ?? "";
                KonditionierungCtrl.Ergebnis e = vorlagen.Uebernehmen(vorlage.Id, ziel, stand.Matrix(idZone));
                if (!e.Ok) return e.Meldung;
            }
            return null;
        }

        /// <summary>
        /// <b>Der gespeicherte Plan derselben Datei</b> (gleicher SHA-256) in diesem Projekt: das jüngste Gebäude des Projekts
        /// aus dieser Datei, seine jüngste Quelle mit diesem Hash, deren Raumpaarungen auf Zonen, die Zonen des Gebäudes in
        /// ihrer Reihenfolge mit Bezeichner und der Nutzung ihrer Kalenderkopien (<see cref="Zonenplan.AusGespeichert"/>).
        /// <c>null</c>, wenn es keinen gibt oder keine Raumpaarung auf eine Zone zeigt.
        /// </summary>
        internal static Zonenplan Gespeichert(int idProjekt, string hash, GebaeudeAbbild abbild, int index,
                                              IReadOnlyDictionary<string, bool> haken = null)
        {
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return null;
            var import = new GebaeudeImportCtrl();
            GebaeudeImportCtrl.ImportTreffer treffer = import.ImporteImProjekt(idProjekt, hash).FirstOrDefault();
            if (treffer == null) return null;
            string h = (hash ?? "").Trim().ToLowerInvariant();
            ImportquelleModel quelle = import.LesenQuellen(treffer.IdGebaeude).FirstOrDefault(q => string.Equals(q.Hash, h, StringComparison.Ordinal));
            if (quelle == null) return null;
            // Die Raumkennungen der Datei über ihre gespeicherte Kurzform (64 Zeichen).
            var jeKurz = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (AbbildRaum r in abbild.Gebaeude[index].Raeume)
                jeKurz.TryAdd(Quellkennung.Kuerzen(r.Kennung), r.Kennung);
            List<(string Raum, int IdZone)> paarungen = import.LesenZuordnungen(quelle.ID)
                .Where(p => p.ID_Zone.HasValue && jeKurz.ContainsKey(p.Quellkennung))
                .Select(p => (jeKurz[p.Quellkennung], p.ID_Zone.Value)).ToList();
            if (paarungen.Count == 0) return null;
            List<(int Id, string Name, string Nutzung)> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(treffer.IdGebaeude)
                .Select(z => (z.ID, z.Bezeichner, Nutzung(z.ID))).ToList();
            return Zonenplan.AusGespeichert(abbild, index, quelle.Zonenregel, haken, zonen, paarungen);
        }

        /// <summary>Die Nutzung der Kalenderkopien einer Zone: die der ersten Größe mit Nutzung; <c>null</c> = keine.</summary>
        internal static string Nutzung(int idZone)
        {
            if (!KonditionierungNutzungSchema.SchemaVollstaendig()) return null;
            object w = DataRepository.ExecuteScalar(
                "SELECT \"Nutzung\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" = ? AND \"Nutzung\" IS NOT NULL " +
                "ORDER BY \"Groesse\", \"ID\" LIMIT 1",
                new DbParam("@z", idZone));
            return w == null || w == DBNull.Value ? null : Convert.ToString(w, CultureInfo.InvariantCulture);
        }
    }
}
