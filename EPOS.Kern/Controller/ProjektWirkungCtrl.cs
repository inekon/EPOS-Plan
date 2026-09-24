using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E17 (V‑G11) — Laden und Speichern der <b>nicht monetarisierbaren Wirkungen</b>
    /// eines Projekts (<c>Tab_ProjektWirkung</c>, Schemaschritt 126). Die Regeln
    /// (Beurteilung, Prüfung, Texte) stehen in <see cref="NichtMonetaereWirkungen"/>.
    ///
    /// <para><b>Die Wirkungen hängen am Stammprojekt</b> — wie bis hierher der Freitext:
    /// Sie gehören zur Maßnahme als Ganzes, nicht zu einer Variante. Seite und Bericht lesen
    /// sie über <c>IdStamm</c>.</para>
    ///
    /// <para><b>Speichern ersetzt die Liste</b> in EINEM Vorgang: löschen, in der gegebenen
    /// Reihenfolge neu schreiben (Sortierung 1, 2, 3 …). Leere Zeilen fallen weg; eine Zeile,
    /// die <see cref="NichtMonetaereWirkungen.Pruefen"/> beanstandet, bricht ab, bevor etwas
    /// geschrieben ist. Der Grund steht dann in <see cref="Speicherfehler"/>.</para>
    ///
    /// <para><b>Keine Rechenwirkung:</b> Kein Rechenweg ruft diesen Controller.</para>
    /// </summary>
    public class ProjektWirkungCtrl
    {
        private const string SQL_LADEN =
            "SELECT \"ID\", \"Sortierung\", \"Kategorie\", \"Beschreibung\", \"Dauer\", " +
            "\"Wirkung_Organisation\", \"Wirkung_Mitarbeiter\", \"Wirkung_Umwelt\" " +
            "FROM \"Tab_ProjektWirkung\" WHERE \"ID_Projekt\" = ? ORDER BY \"Sortierung\", \"ID\"";

        private const string SQL_LOESCHEN =
            "DELETE FROM \"Tab_ProjektWirkung\" WHERE \"ID_Projekt\" = ?";

        private const string SQL_EINFUEGEN =
            "INSERT INTO \"Tab_ProjektWirkung\" (\"ID_Projekt\", \"Sortierung\", \"Kategorie\", \"Beschreibung\", " +
            "\"Dauer\", \"Wirkung_Organisation\", \"Wirkung_Mitarbeiter\", \"Wirkung_Umwelt\") " +
            "VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

        /// <summary>Der Grund des zuletzt gescheiterten Ladens; <c>null</c> = keiner.</summary>
        public string Ladefehler { get; private set; }

        /// <summary>Der Grund des zuletzt gescheiterten Speicherns; <c>null</c> = keiner.</summary>
        public string Speicherfehler { get; private set; }

        /// <summary>
        /// Die Wirkungen des Projekts in ihrer Reihenfolge. Ein Lesefehler (etwa eine Datei vor
        /// Schritt 126) liefert die leere Liste und setzt <see cref="Ladefehler"/>.
        /// </summary>
        public List<ProjektWirkung> Laden(int idProjekt)
        {
            Ladefehler = null;
            var liste = new List<ProjektWirkung>();
            if (idProjekt <= 0) return liste;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    DataTable t = v.Lese(SQL_LADEN, new DbParam("@p", idProjekt));
                    foreach (DataRow r in t.Rows)
                    {
                        liste.Add(new ProjektWirkung
                        {
                            Id = Ganz(r["ID"]) ?? 0,
                            Sortierung = Ganz(r["Sortierung"]) ?? 0,
                            Kategorie = Convert.ToString(r["Kategorie"]) ?? NichtMonetaereWirkungen.SONSTIG,
                            Beschreibung = r["Beschreibung"] == DBNull.Value ? "" : Convert.ToString(r["Beschreibung"]) ?? "",
                            Dauer = Ganz(r["Dauer"]),
                            WirkungOrganisation = Ganz(r["Wirkung_Organisation"]),
                            WirkungMitarbeiter = Ganz(r["Wirkung_Mitarbeiter"]),
                            WirkungUmwelt = Ganz(r["Wirkung_Umwelt"])
                        });
                    }
                    v.Commit();
                }
            }
            catch (Exception ex)
            {
                Ladefehler = Fehlergrund.Text(ex);
                liste.Clear();
            }
            return liste;
        }

        /// <summary>
        /// Ersetzt die Wirkungen des Projekts durch <paramref name="liste"/> (Reihenfolge =
        /// Sortierung). <c>true</c> = geschrieben; sonst steht der Grund in
        /// <see cref="Speicherfehler"/> und die Datenbank ist unverändert.
        /// </summary>
        public bool Speichern(int idProjekt, IEnumerable<ProjektWirkung> liste)
        {
            Speicherfehler = null;
            if (idProjekt <= 0)
            {
                Speicherfehler = MyResource.Resource.WIRT_NM_FEHLER_PROJEKT;
                return false;
            }

            List<ProjektWirkung> zeilen = (liste ?? Enumerable.Empty<ProjektWirkung>())
                .Where(w => !NichtMonetaereWirkungen.IstLeer(w)).ToList();
            foreach (ProjektWirkung w in zeilen)
            {
                string befund = NichtMonetaereWirkungen.Pruefen(w);
                if (befund != null) { Speicherfehler = befund; return false; }
            }

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren(SQL_LOESCHEN, new DbParam("@p", idProjekt));
                    int nr = 0;
                    foreach (ProjektWirkung w in zeilen)
                    {
                        nr++;
                        v.Ausfuehren(SQL_EINFUEGEN,
                            new DbParam("@p", idProjekt),
                            new DbParam("@s", nr),
                            new DbParam("@k", w.Kategorie),
                            new DbParam("@b", w.Beschreibung.Trim()),
                            Wert("@d", w.Dauer),
                            Wert("@o", w.WirkungOrganisation),
                            Wert("@m", w.WirkungMitarbeiter),
                            Wert("@u", w.WirkungUmwelt));
                    }
                    v.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                Speicherfehler = Fehlergrund.Text(ex);
                return false;
            }
        }

        private static DbParam Wert(string name, int? wert)
            => new DbParam(name, DbParamTyp.Integer) { Wert = wert.HasValue ? (object)wert.Value : DBNull.Value };

        private static int? Ganz(object o)
            => o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o);
    }
}
