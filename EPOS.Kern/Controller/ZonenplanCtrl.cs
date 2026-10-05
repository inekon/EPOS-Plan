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
        /// <b>Gibt einer Zone die Vorlagen ihrer Nutzung</b> über den Weg von „Vorlage übernehmen“ (KP2): je Größe die erste
        /// ausgelieferte Vorlage dieser Nutzung in der Ordnung der Auswahlliste, als reiner Schritt
        /// (<see cref="Konditionierungsarbeit.VorlageUebernehmen"/>: Kalenderkopie mit den Ferien der Zone, Folgen) auf dem
        /// Arbeitsstand des Gebäudes; danach werden Gebäude- und Zonenebene geschrieben und die Kopien tragen die Nutzung.
        /// Eine unbeheizte Zone bekommt keinen Heiz- und Kühlkalender (Zonenregel), eine Größe ohne Vorlage dieser Nutzung
        /// keinen Kalender. Fragt die Lüftung nach dem Aufteilen der Gesamtangabe <c>Luftwechselrate</c> (F5), wird
        /// aufgeteilt — der wirksame Luftwechsel bleibt. Läuft im Vorgang des Aufrufers, wenn einer angemeldet ist
        /// (<see cref="Vorgangsklammer"/>, Sicherungspunkt).
        /// </summary>
        /// <returns><c>null</c>, wenn alles übernommen ist; sonst die erste Ablehnung.</returns>
        internal static string NutzungUebernehmen(int idGebaeude, int idZone, string nutzung)
        {
            if (nutzung == null) return null;
            if (!Zonenplan.NUTZUNGEN.Contains(nutzung))
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.IMP_IFC_PROT_PLAN_NUTZUNG_UNGUELTIG, nutzung);
            var vorlagen = new KonditionierungsvorlageCtrl();
            var kond = new KonditionierungCtrl();
            Konditionierungsarbeitsstand stand = kond.ArbeitsstandLesen(idGebaeude, null, out string gelesen);
            Konditionierungszone zone = stand?.Zone(idZone);
            if (zone == null) return gelesen ?? "";
            var uebernommen = new List<Konditionierungsgroesse>();
            foreach (Konditionierungsgroesse groesse in Konditionierungsgroessen.Alle)
            {
                if (!zone.IstBeheizt && (groesse == Konditionierungsgroesse.Heizsoll || groesse == Konditionierungsgroesse.Kuehlsoll)) continue;
                KonditionierungsvorlageCtrl.Vorlage kopf = vorlagen.Liste(groesse)
                    .FirstOrDefault(x => x.Ausgeliefert && string.Equals(x.Nutzung, nutzung, StringComparison.Ordinal));
                if (kopf == null) continue;
                Konditionierungsstand inhalt = kond.StandLesen(KonditionierungCtrl.Eigner.Vorlage(kopf.Id), out string m);
                if (m != null) return m;
                var vorlage = new Konditionierungsvorlage(kopf.Id, kopf.Bezeichner, kopf.Groesse, inhalt);
                var ort = new Konditionierungsort(groesse, idZone);
                Konditionierungsschritt schritt = Konditionierungsarbeit.VorlageUebernehmen(stand, ort, vorlage);
                if (schritt.Rueckfrage)
                {
                    Konditionierungsschritt geteilt = Konditionierungsarbeit.LuftwechselAufteilen(stand);
                    if (!geteilt.Ok) return geteilt.Meldung;
                    schritt = Konditionierungsarbeit.VorlageUebernehmen(geteilt.Stand, ort, vorlage);
                }
                if (!schritt.Ok || schritt.Rueckfrage) return schritt.Meldung ?? "";
                stand = schritt.Stand;
                uebernommen.Add(groesse);
            }
            if (uebernommen.Count == 0) return null;
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                KonditionierungCtrl.Eigner ziel = KonditionierungCtrl.Eigner.Zone(idGebaeude, idZone);
                KonditionierungCtrl.Ergebnis e = kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(idGebaeude), stand.Gebaeude, true, out _);
                if (e.Ok) e = kond.StandSchreiben(v, ziel, stand.Zone(idZone).Stand, true, out _);
                if (!e.Ok)
                {
                    v.Rollback();
                    return e.Meldung;
                }
                // Die Kopie trägt die Nutzung der Vorlage selbst (wie „Vorlage übernehmen").
                foreach (Konditionierungsgroesse groesse in uebernommen) KonditionierungCtrl.NutzungSetzen(ziel, groesse, nutzung);
                v.Commit();
            }
            return null;
        }

        /// <summary>
        /// <b>Gibt einer Zone die Konditionierung aus der HottCAD-Projektdatei</b> (Datenaustauschkonzept 16.3, SQ-1) — nach
        /// <see cref="NutzungUebernehmen"/> und über dieselben reinen Schritte der Konditionierung: je Größe, für die die
        /// Projektdatei etwas liefert, weicht zuerst die Kalenderkopie der Vorlage (Rangfolge „Profil vor Vorlage“), dann
        /// setzt <see cref="Konditionierungsarbeit.ZelleSetzen"/> die Zellen des Nutzungsprofils (Bestand oder Vorgabetabelle,
        /// wie die Matrix es verlangt) und zuletzt tritt der Kalender der Ganglinie an (Rangfolge „Ganglinie vor Profil“) —
        /// mit dem Beleg in <c>Bemerkung</c> (≤ 200 Zeichen). Was die Projektdatei nicht liefert, bleibt bei der Vorlage der
        /// Nutzung. Eine unbeheizte Zone bekommt keinen Heiz- und Kühlwert (Zonenregel). Läuft im Vorgang des Aufrufers, wenn
        /// einer angemeldet ist (<see cref="Vorgangsklammer"/>, Sicherungspunkt).
        /// </summary>
        /// <param name="idZone">Die Zone; <c>null</c> = das Gebäude selbst (Einzonenweg: Gebäudekalender ohne Zone,
        /// Konditionierungskonzept 5.1).</param>
        /// <returns><c>null</c>, wenn alles übernommen ist; sonst die erste Ablehnung.</returns>
        internal static string ProjektdateiUebernehmen(int idGebaeude, int? idZone, Zonenkonditionierung konditionierung)
        {
            if (konditionierung == null || !konditionierung.Liefert) return null;
            var kond = new KonditionierungCtrl();
            Konditionierungsarbeitsstand stand = kond.ArbeitsstandLesen(idGebaeude, null, out string gelesen);
            if (stand == null) return gelesen ?? "";
            Konditionierungszone zone = idZone is int iz ? stand.Zone(iz) : null;
            if (idZone.HasValue && zone == null) return gelesen ?? "";
            Konditionierungsstand Ebene(Konditionierungsarbeitsstand s) => idZone is int z ? s.Zone(z).Stand : s.Gebaeude;
            var kalender = new List<Konditionierungsgroesse>();
            int geschrieben = 0;
            foreach (Groessenkonditionierung g in konditionierung.Groessen)
            {
                if (g.Herkunft == Konditionierungsherkunft.Vorlage) continue;
                if (zone != null && !zone.IstBeheizt && (g.Groesse == Konditionierungsgroesse.Heizsoll || g.Groesse == Konditionierungsgroesse.Kuehlsoll)) continue;
                var ort = new Konditionierungsort(g.Groesse, idZone);
                if (Ebene(stand).Kalender(g.Groesse) != null)
                {
                    Konditionierungsschritt weg = Konditionierungsarbeit.Verwerfen(stand, ort);
                    if (!weg.Ok) return weg.Meldung ?? "";
                    stand = weg.Stand;
                }
                foreach (Vorgabebeleg v in g.Vorgaben)
                {
                    Konditionierungsschritt schritt = Konditionierungsarbeit.ZelleSetzen(stand, ort, v.Zeile, v.Zelle);
                    if (schritt.Rueckfrage)
                    {
                        Konditionierungsschritt geteilt = Konditionierungsarbeit.LuftwechselAufteilen(stand);
                        if (!geteilt.Ok) return geteilt.Meldung;
                        schritt = Konditionierungsarbeit.ZelleSetzen(geteilt.Stand, ort, v.Zeile, v.Zelle);
                    }
                    if (!schritt.Ok || schritt.Rueckfrage) return schritt.Meldung ?? "";
                    stand = schritt.Stand;
                }
                if (g.Kalender != null)
                {
                    Konditionierungsstand ebene = Ebene(stand);
                    stand = stand.MitEbene(idZone, ebene.MitKalender(g.Groesse, g.Kalender, new Kalenderherkunft(null, g.Bemerkung)));
                    kalender.Add(g.Groesse);
                }
                geschrieben++;
            }
            if (geschrieben == 0) return null;
            using (DbVorgang v = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
            {
                KonditionierungCtrl.Eigner ziel = idZone is int z ? KonditionierungCtrl.Eigner.Zone(idGebaeude, z) : KonditionierungCtrl.Eigner.Gebaeude(idGebaeude);
                KonditionierungCtrl.Ergebnis e = kond.StandSchreiben(v, KonditionierungCtrl.Eigner.Gebaeude(idGebaeude), stand.Gebaeude, true, out _);
                if (e.Ok && idZone.HasValue) e = kond.StandSchreiben(v, ziel, Ebene(stand), true, out _);
                if (!e.Ok)
                {
                    v.Rollback();
                    return e.Meldung;
                }
                if (konditionierung.Nutzung != null)
                    foreach (Konditionierungsgroesse groesse in kalender) KonditionierungCtrl.NutzungSetzen(ziel, groesse, konditionierung.Nutzung);
                v.Commit();
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
