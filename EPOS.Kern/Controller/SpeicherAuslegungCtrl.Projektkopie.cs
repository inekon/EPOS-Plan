using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die benannte Naht der Projektkopie</b> (Auftrag VF-1): die Versätze, mit denen
    /// <see cref="ProjektDuplizierenCtrl"/> die ID-SPALTEN der Kopie gebildet hat.
    ///
    /// <para>Der Duplizierer versetzt jede ID-Spalte mit dem Offset ihrer Zieltabelle.
    /// Kennungen, die INNERHALB eines JSON-Standes stehen, erreicht er damit nicht — er
    /// sieht dort nur eine <c>TEXT</c>-Spalte. Statt ihm die Modelle beizubringen oder ein
    /// Wörterbuch durch fremde Schichten zu reichen, gibt er die zwei Versätze weiter, die
    /// ein JSON-Stand braucht; der zuständige Controller zieht seine eigenen Bezüge nach.</para>
    /// </summary>
    public sealed class ProjektkopieVersatz
    {
        /// <summary><c>Tab_Projekt.ID</c> der fertigen Kopie.</summary>
        public int ZielProjektId { get; init; }

        /// <summary>Versatz der <c>Tab_Energieanlagen.ID</c> (Quellzeile + Versatz = Kopie).</summary>
        public long Anlagen { get; init; }

        /// <summary>Versatz der <c>Tab_Kostenprofil.ID</c> (Quellzeile + Versatz = Kopie).</summary>
        public long Kostenprofile { get; init; }
    }

    public static partial class SpeicherAuslegungCtrl
    {
        /// <summary>
        /// Zieht die Kennungen INNERHALB der Auslegungsprofile einer frischen Projektkopie
        /// auf die Zeilen der KOPIE nach (Auftrag VF-1, Messung MV-1 vom 17.09.2026).
        ///
        /// <para><b>Der Befund.</b> <c>Tab_SpeicherAuslegung</c> wandert mitsamt
        /// <c>@Projektflotte</c> in die Variante, und ihre ID-Spalten versetzt der
        /// Duplizierer. Die Spalte <c>Daten</c> ist für ihn aber ein Textfeld — und in ihr
        /// stehen zwei Bezüge auf Projektzeilen: <see cref="FlottenEinheit.AnlageId"/> (die
        /// vertretene Speicheranlage) und die eingefrorene Kopie des Strompreisprofils
        /// (<see cref="KostenprofilModel.ID"/> samt <c>ID_Projekt</c>). Beide zeigten in der
        /// Kopie weiter auf das QUELLprojekt.</para>
        ///
        /// <para><b>Rechnerisch folgenlos, in der Bedienung nicht.</b> Die Einheiten tragen
        /// Physik und Kosten selbst, und der Flottenlauf liest <c>AnlageId</c> nicht. Wer die
        /// Einheiten der Kopie aber in ihre Projektanlagen übernimmt
        /// (<c>SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen</c>), fand die fremde
        /// Anlage im eigenen Projekt nicht und bekam eine ZWEITE Anlage statt der Änderung
        /// der vorhandenen.</para>
        ///
        /// <para><b>Unauflösbar heißt <c>null</c>, nicht „irgendeine".</b> Ergibt der Versatz
        /// keine Zeile des Zielprojekts — etwa weil die vertretene Anlage im Quellprojekt
        /// zwischenzeitlich gelöscht wurde —, wird der Bezug gelöscht und protokolliert. Eine
        /// Einheit ohne Anlagenbezug ist ein gültiger Zustand (eine nur im Studienstand
        /// geführte Einheit); ein Bezug auf ein fremdes Projekt ist keiner.</para>
        /// </summary>
        /// <returns>Die Protokollzeilen der gelösten Bezüge; leer = alles aufgelöst.</returns>
        public static IReadOnlyList<string> KopieBezuegeNachziehen(ProjektkopieVersatz versatz)
        {
            var protokoll = new List<string>();
            if (versatz == null || versatz.ZielProjektId <= 0) return protokoll;

            SchemaSicherstellen();

            DataTable staende = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Daten FROM Tab_SpeicherAuslegung WHERE ID_Projekt = ? ORDER BY ID",
                new DbParam("@p", versatz.ZielProjektId));
            if (staende == null || staende.Rows.Count == 0) return protokoll;

            HashSet<long> anlagen = Kennungen(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ?", versatz.ZielProjektId);
            HashSet<long> profile = Kennungen(
                "SELECT ID FROM Tab_Kostenprofil WHERE ID_Projekt = ?", versatz.ZielProjektId);

            foreach (DataRow r in staende.Rows)
            {
                int id = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture);
                string name = Convert.ToString(r["Bezeichner"]) ?? "";

                SpeicherOptimierungEingaben eingaben;
                try { eingaben = Deserialisieren(Convert.ToString(r["Daten"])); }
                catch (Exception ex)
                {
                    protokoll.Add(Zeile(versatz, name, "der Stand ist nicht lesbar: " + ex.Message));
                    continue;
                }

                SpeicherAuslegungKonfiguration a = eingaben?.Auslegung;
                if (a == null) continue;

                bool geaendert = EinheitenNachziehen(a.Flotte, versatz, anlagen, name, protokoll);
                if (StrompreisprofilNachziehen(a.Strompreisprofil, versatz, profile, name, protokoll))
                    geaendert = true;
                if (!geaendert) continue;

                DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_SpeicherAuslegung SET Daten = ? WHERE ID = ?",
                    new DbParam("@d", Serialisieren(eingaben)),
                    new DbParam("@id", id));
            }

            foreach (string z in protokoll) Protokoll(z);
            return protokoll;
        }

        /// <summary>Bildet jede <see cref="FlottenEinheit.AnlageId"/> über den Anlagenversatz ab.</summary>
        private static bool EinheitenNachziehen(FlottenStudieKonfiguration flotte,
                                                ProjektkopieVersatz versatz, HashSet<long> anlagen,
                                                string stand, List<string> protokoll)
        {
            IList<FlottenEinheit> einheiten = flotte?.Einheiten;
            if (einheiten == null || einheiten.Count == 0) return false;

            bool geaendert = false;
            foreach (FlottenEinheit e in einheiten)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.AnlageId)) continue;

                long alt;
                if (!long.TryParse(e.AnlageId, NumberStyles.Integer, CultureInfo.InvariantCulture, out alt))
                {
                    protokoll.Add(Zeile(versatz, stand, "die Einheit „" + Benennung(e) +
                        "“ führt keinen Anlagenschlüssel („" + e.AnlageId + "“); der Bezug entfällt"));
                    e.AnlageId = null;
                    geaendert = true;
                    continue;
                }

                long neu = alt + versatz.Anlagen;
                if (anlagen.Contains(neu))
                {
                    if (neu != alt)
                    {
                        e.AnlageId = neu.ToString(CultureInfo.InvariantCulture);
                        geaendert = true;
                    }
                    continue;
                }

                protokoll.Add(Zeile(versatz, stand, "die Einheit „" + Benennung(e) + "“ vertrat Anlage " +
                    alt.ToString(CultureInfo.InvariantCulture) + "; die Kopie führt dazu keine Anlage (" +
                    neu.ToString(CultureInfo.InvariantCulture) + "); der Bezug entfällt"));
                e.AnlageId = null;
                geaendert = true;
            }
            return geaendert;
        }

        /// <summary>
        /// Zieht die eingefrorene Kopie des Strompreisprofils auf die Profilzeile der KOPIE
        /// nach. Die WERTE des Profils stehen im Stand selbst und bleiben unangetastet; nur
        /// sein Anker wandert mit.
        /// </summary>
        private static bool StrompreisprofilNachziehen(KostenprofilModel profil,
                                                       ProjektkopieVersatz versatz, HashSet<long> profile,
                                                       string stand, List<string> protokoll)
        {
            if (profil == null || profil.ID <= 0) return false;

            long neu = profil.ID + versatz.Kostenprofile;
            if (profile.Contains(neu))
            {
                if (neu == profil.ID && profil.ID_Projekt == versatz.ZielProjektId) return false;
                profil.ID = (int)neu;
                profil.ID_Projekt = versatz.ZielProjektId;
                return true;
            }

            protokoll.Add(Zeile(versatz, stand, "das eingefrorene Strompreisprofil „" +
                (profil.Bezeichner ?? "") + "“ (Zeile " + profil.ID.ToString(CultureInfo.InvariantCulture) +
                ") hat in der Kopie keine Entsprechung; der Bezug entfällt, die Werte bleiben"));
            profil.ID = 0;
            profil.ID_Projekt = versatz.ZielProjektId;
            return true;
        }

        private static string Benennung(FlottenEinheit e)
            => !string.IsNullOrWhiteSpace(e.Name) ? e.Name
             : !string.IsNullOrWhiteSpace(e.Id) ? e.Id : "?";

        private static HashSet<long> Kennungen(string sql, int projektId)
        {
            var menge = new HashSet<long>();
            try
            {
                DataTable t = DataRepository.GetDataTable(sql, new DbParam("@p", projektId));
                if (t == null) return menge;
                foreach (DataRow r in t.Rows)
                    if (r[0] != DBNull.Value) menge.Add(Convert.ToInt64(r[0], CultureInfo.InvariantCulture));
            }
            catch { }
            return menge;
        }

        private static string Zeile(ProjektkopieVersatz versatz, string stand, string was)
            => "Projekt " + versatz.ZielProjektId.ToString(CultureInfo.InvariantCulture) +
               ", Auslegungsstand „" + stand + "“: " + was + ".";

        /// <summary>Protokolliert einen gelösten Bezug, ohne den Anwender zu stören.</summary>
        private static void Protokoll(string meldung)
        {
            try { Console.WriteLine("SpeicherAuslegungCtrl.KopieBezuegeNachziehen: " + meldung); }
            catch { }
        }
    }
}
