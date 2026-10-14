using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Schreibweg der pflegbaren Kältefolge</b> (Welle KB-D, Entscheid E117 F1; Entwurf Kältebereich 3.4): setzt den
    /// Rang der Kälteerzeuger eines Projekts (<c>Tab_Energieanlagen.Kaelte_Rang</c>, Schemaschritt
    /// <see cref="KaelteRangSchema.SCHRITT"/>) und stellt die Vorgabefolge wieder her. Gelesen wird der Rang allein über
    /// <see cref="Kaeltefolge"/> — Lauf, Schema und Bereich „Kälte“ ordnen damit.
    ///
    /// <para><b>Prüfregeln</b> (<see cref="Pruefen"/>): Eine gepflegte Folge nennt jeden Kälteerzeuger des Projekts — die
    /// Wärmepumpen mit Kühlfunktion und die Kältemaschinen-Anlagen, wie <see cref="Kaeltefolge.Lesen"/> sie liefert — genau
    /// einmal und keine andere Anlage. Die Ränge werden lückenlos ab 1 geschrieben und sind damit eindeutig; jede andere
    /// Anlage des Projekts steht danach ohne Rang. Die Stufen bleiben fest (freie Kühlung vorn, Kältespeicher nach
    /// Entladepriorität); die Wärmekaskade berührt der Rang nicht.</para>
    ///
    /// <para><b>Rückgabe</b> aller Schreibwege: <c>null</c> = geschrieben, sonst der Grund in der Oberflächensprache.
    /// Dialogfrei.</para>
    /// </summary>
    public static class KaeltefolgeCtrl
    {
        /// <summary>Richtung für <see cref="Verschieben"/>: einen Platz nach vorn (früher decken).</summary>
        public const int NACH_VORN = -1;

        /// <summary>Richtung für <see cref="Verschieben"/>: einen Platz nach hinten (später decken).</summary>
        public const int NACH_HINTEN = 1;

        /// <summary>Die Anlagen-IDs der Kälteerzeuger des Projekts in der Folge des Laufs.</summary>
        public static List<int> FolgeLesen(int idProjekt)
            => Kaeltefolge.Lesen(idProjekt).Erzeuger.Select(e => e.AnlagenId).ToList();

        /// <summary>
        /// Prüft eine Folge gegen die Kälteerzeuger des Projekts; <c>null</c> = stimmig. Eine leere Folge ist stimmig (sie
        /// heißt Vorgabefolge).
        /// </summary>
        public static string Pruefen(int idProjekt, IReadOnlyList<int> folge)
        {
            if (!KaelteRangSchema.Vollstaendig())
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_SCHEMA,
                                     KaelteRangSchema.SCHRITT);
            if (idProjekt <= 0) return MyResource.Resource.KAELTEFOLGE_FEHLER_KEIN_PROJEKT;
            if (folge == null || folge.Count == 0) return null;

            List<KaelteerzeugerEintrag> erzeuger = Kaeltefolge.Lesen(idProjekt).Erzeuger.ToList();
            var bekannt = erzeuger.ToDictionary(e => e.AnlagenId);
            var gesehen = new HashSet<int>();
            foreach (int id in folge)
            {
                if (!bekannt.ContainsKey(id))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_KEIN_KAELTEERZEUGER,
                                         id.ToString(CultureInfo.CurrentCulture));
                if (!gesehen.Add(id))
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_DOPPELT,
                                         bekannt[id].Bezeichner);
            }
            KaelteerzeugerEintrag fehlt = erzeuger.FirstOrDefault(e => !gesehen.Contains(e.AnlagenId));
            if (fehlt != null)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_UNVOLLSTAENDIG,
                                     fehlt.Bezeichner);
            return null;
        }

        /// <summary>
        /// Schreibt die Folge: Rang 1 … n in der Folge von <paramref name="folge"/> (Anlagen-IDs), jede andere Anlage des
        /// Projekts ohne Rang — in einem Vorgang. Eine leere Folge stellt die Vorgabefolge her.
        /// </summary>
        public static string FolgeSetzen(int idProjekt, IReadOnlyList<int> folge)
        {
            string grund = Pruefen(idProjekt, folge);
            if (grund != null) return grund;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("UPDATE Tab_Energieanlagen SET Kaelte_Rang = NULL WHERE ID_Projekt = ? AND Kaelte_Rang IS NOT NULL",
                                 new DbParam("?", idProjekt));
                    for (int i = 0; folge != null && i < folge.Count; i++)
                        v.Ausfuehren("UPDATE Tab_Energieanlagen SET Kaelte_Rang = ? WHERE ID = ? AND ID_Projekt = ?",
                                     new DbParam("?", i + 1), new DbParam("?", folge[i]), new DbParam("?", idProjekt));
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_SCHREIBEN,
                                         ex.Message);
                }
            }
            return null;
        }

        /// <summary>
        /// Rückt einen Kälteerzeuger einen Platz nach vorn (<see cref="NACH_VORN"/>) oder hinten (<see cref="NACH_HINTEN"/>)
        /// und schreibt die ganze Folge als gepflegte (<see cref="FolgeSetzen"/>).
        /// </summary>
        public static string Verschieben(int idProjekt, int idAnlage, int richtung)
        {
            if (richtung != NACH_VORN && richtung != NACH_HINTEN)
                throw new ArgumentOutOfRangeException(nameof(richtung), richtung, "NACH_VORN oder NACH_HINTEN.");
            List<int> folge = FolgeLesen(idProjekt);
            int platz = folge.IndexOf(idAnlage);
            if (platz < 0)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_KEIN_KAELTEERZEUGER,
                                     idAnlage.ToString(CultureInfo.CurrentCulture));
            int ziel = platz + richtung;
            if (ziel < 0 || ziel >= folge.Count) return MyResource.Resource.KAELTEFOLGE_FEHLER_RAND;
            (folge[platz], folge[ziel]) = (folge[ziel], folge[platz]);
            return FolgeSetzen(idProjekt, folge);
        }

        /// <summary>Stellt die Vorgabefolge her: kein Kälteerzeuger des Projekts trägt danach einen Rang.</summary>
        public static string VorgabeSetzen(int idProjekt) => FolgeSetzen(idProjekt, Array.Empty<int>());

        /// <summary>
        /// <b>Hängt Anlagen hinter die Erzeuger mit Rang</b> (KM‑K1, Anwenderentscheid 12.10.2026): Jede genannte Anlage ohne
        /// Rang bekommt den nächsten Rang nach dem größten des Projekts — in der genannten Folge; eine Anlage mit Rang behält
        /// ihn. Ohne gepflegte Folge steht sie damit vor allen Erzeugern ohne Rang. Die übrigen Ränge bleiben unberührt.
        /// </summary>
        public static string RangAnhaengen(int idProjekt, IEnumerable<int> anlagen)
        {
            if (!KaelteRangSchema.Vollstaendig())
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_SCHEMA, KaelteRangSchema.SCHRITT);
            if (idProjekt <= 0) return MyResource.Resource.KAELTEFOLGE_FEHLER_KEIN_PROJEKT;
            Dictionary<int, int> raenge = Kaeltefolge.RaengeLesen(idProjekt);
            var neu = new Dictionary<int, int>(raenge);
            int naechster = raenge.Count == 0 ? 1 : raenge.Values.Max() + 1;
            foreach (int id in (anlagen ?? Array.Empty<int>()).Distinct())
                if (id > 0 && !neu.ContainsKey(id)) neu[id] = naechster++;
            return neu.Count == raenge.Count ? null : RaengeSchreiben(idProjekt, neu);
        }

        /// <summary>
        /// <b>Nimmt den Rang der genannten Anlagen weg und schließt die Folge</b>: Die übrigen Erzeuger mit Rang rücken in
        /// ihrer Reihenfolge lückenlos ab 1 nach.
        /// </summary>
        public static string RangEntfernen(int idProjekt, IEnumerable<int> anlagen)
        {
            if (!KaelteRangSchema.Vollstaendig())
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_SCHEMA, KaelteRangSchema.SCHRITT);
            if (idProjekt <= 0) return MyResource.Resource.KAELTEFOLGE_FEHLER_KEIN_PROJEKT;
            Dictionary<int, int> raenge = Kaeltefolge.RaengeLesen(idProjekt);
            var weg = new HashSet<int>(anlagen ?? Array.Empty<int>());
            if (!raenge.Keys.Any(weg.Contains)) return null;
            int platz = 1;
            var neu = raenge.Where(p => !weg.Contains(p.Key)).OrderBy(p => p.Value).ThenBy(p => p.Key)
                            .ToDictionary(p => p.Key, _ => platz++);
            return RaengeSchreiben(idProjekt, neu);
        }

        /// <summary>
        /// Schreibt einen gemerkten Stand der Ränge zurück (Abbrechen eines Dialogs): genau diese Ränge, jede andere Anlage
        /// des Projekts ohne Rang; Anlagen, die es nicht mehr gibt, fallen weg.
        /// </summary>
        public static string RaengeWiederherstellen(int idProjekt, IReadOnlyDictionary<int, int> raenge)
        {
            if (!KaelteRangSchema.Vollstaendig() || idProjekt <= 0) return null;
            return RaengeSchreiben(idProjekt, raenge ?? new Dictionary<int, int>());
        }

        private static string RaengeSchreiben(int idProjekt, IReadOnlyDictionary<int, int> raenge)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("UPDATE Tab_Energieanlagen SET Kaelte_Rang = NULL WHERE ID_Projekt = ? AND Kaelte_Rang IS NOT NULL",
                                 new DbParam("?", idProjekt));
                    foreach (KeyValuePair<int, int> p in raenge)
                        v.Ausfuehren("UPDATE Tab_Energieanlagen SET Kaelte_Rang = ? WHERE ID = ? AND ID_Projekt = ?",
                                     new DbParam("?", p.Value), new DbParam("?", p.Key), new DbParam("?", idProjekt));
                    v.Commit();
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KAELTEFOLGE_FEHLER_SCHREIBEN, ex.Message);
                }
            }
            return null;
        }
    }
}
