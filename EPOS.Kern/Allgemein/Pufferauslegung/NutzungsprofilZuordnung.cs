using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>Woher ein Schlüssel der Nutzungsprofil-Zuordnung stammt (<c>Z_Nutzungsprofil.Quelle</c>).</summary>
    public static class NutzungsprofilQuelle
    {
        /// <summary>Der Bezeichner einer Zapf-Nutzungsart (<c>Tab_TwwNutzungsart_STAMM.Bezeichner</c>, natürlicher Schlüssel des Katalogs).</summary>
        public const string ZAPF = "ZAPF";
        /// <summary>Die <c>Nutzung</c> einer Konditionierungsvorlage (WOHNEN, BUERO, SCHULE, SONSTIGE).</summary>
        public const string KONDITIONIERUNG = "KONDITIONIERUNG";
        /// <summary>Die Gebäudeart (<c>Tab_Gebaeude.Gebaeudeart</c>), getrimmt.</summary>
        public const string GEBAEUDEART = "GEBAEUDEART";

        /// <summary>Alle Quellen — die Wertemenge der Prüfklausel.</summary>
        public static readonly IReadOnlyList<string> ALLE = new[] { ZAPF, KONDITIONIERUNG, GEBAEUDEART };
    }

    /// <summary>
    /// <b>Die Zuordnung Schlüssel → Nutzungsprofil</b> (V32, Recherche Pufferoptimierung 4.2): eine Tabelle
    /// statt Textvergleichen. <see cref="VORGABE"/> ist die EINE Quelle im Code — aus ihr sät der
    /// Schemaschritt <see cref="ProzessNutzungSchema"/> <c>Z_Nutzungsprofil</c>, und auf sie fällt
    /// <see cref="Lesen"/> zurück, wenn die Tabelle fehlt. Verglichen wird immer der ganze Schlüssel
    /// (getrimmt, Groß-/Kleinschreibung gleich), nie ein Teiltext.
    /// </summary>
    public static class NutzungsprofilZuordnung
    {
        /// <summary>Die ausgelieferte Zuordnung (Quelle, Schlüssel, Profil).</summary>
        public static readonly IReadOnlyList<(string Quelle, string Schluessel, PufferNutzungsprofil Profil)> VORGABE = new[]
        {
            // Zapf-Nutzungsarten des freien Paketteils (Katalogpaket_frei)
            (NutzungsprofilQuelle.ZAPF, "Wohnen groß (abgeleitet)", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.ZAPF, "Ein- und Zweifamilienhaus (abgeleitet)", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.ZAPF, "Studentenwohnheim (abgeleitet)", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.ZAPF, "Seniorenheim (abgeleitet)", PufferNutzungsprofil.PFLEGE),
            (NutzungsprofilQuelle.ZAPF, "Krankenhaus (abgeleitet)", PufferNutzungsprofil.PFLEGE),
            (NutzungsprofilQuelle.ZAPF, "Hotel (aus Messung, je Zimmer)", PufferNutzungsprofil.BEHERBERGUNG),
            (NutzungsprofilQuelle.ZAPF, "Büro (Setzung)", PufferNutzungsprofil.BUERO_SCHULE),
            (NutzungsprofilQuelle.ZAPF, "Schule (Setzung)", PufferNutzungsprofil.BUERO_SCHULE),
            (NutzungsprofilQuelle.ZAPF, "Gewerbe Schichtbetrieb (Setzung)", PufferNutzungsprofil.GEWERBE),
            // Nutzung der Konditionierungsvorlagen: allein Büro und Schule wirken (WOHNEN, SONSTIGE = Vorgabe)
            (NutzungsprofilQuelle.KONDITIONIERUNG, "BUERO", PufferNutzungsprofil.BUERO_SCHULE),
            (NutzungsprofilQuelle.KONDITIONIERUNG, "SCHULE", PufferNutzungsprofil.BUERO_SCHULE),
            // Gebäudearten des Katalogs (Freitext, ganzer Schlüssel)
            (NutzungsprofilQuelle.GEBAEUDEART, "Einfamilienhaus", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.GEBAEUDEART, "Mehrfamilienhaus", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.GEBAEUDEART, "kleines Mehrfamilienhaus", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.GEBAEUDEART, "grosses Mehrfamilienhaus", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.GEBAEUDEART, "Reiheneckhaus", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.GEBAEUDEART, "Reihenmittelhaus", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.GEBAEUDEART, "Wohnblock", PufferNutzungsprofil.WOHNEN),
            (NutzungsprofilQuelle.GEBAEUDEART, "Hotel", PufferNutzungsprofil.BEHERBERGUNG),
            (NutzungsprofilQuelle.GEBAEUDEART, "Altenheim", PufferNutzungsprofil.PFLEGE),
            (NutzungsprofilQuelle.GEBAEUDEART, "Krankenhaus", PufferNutzungsprofil.PFLEGE),
            (NutzungsprofilQuelle.GEBAEUDEART, "Schule", PufferNutzungsprofil.BUERO_SCHULE),
            (NutzungsprofilQuelle.GEBAEUDEART, "Verwaltung", PufferNutzungsprofil.BUERO_SCHULE),
            (NutzungsprofilQuelle.GEBAEUDEART, "Verwaltungsgebäude", PufferNutzungsprofil.BUERO_SCHULE),
            (NutzungsprofilQuelle.GEBAEUDEART, "Gewerbe", PufferNutzungsprofil.GEWERBE),
            (NutzungsprofilQuelle.GEBAEUDEART, "Industriehalle", PufferNutzungsprofil.GEWERBE),
        };

        private static string Schluessel(string quelle, string schluessel)
            => quelle + "\u001f" + (schluessel ?? "").Trim().ToUpperInvariant();

        /// <summary>Die Zuordnung als Nachschlagetabelle aus einer Liste von Einträgen.</summary>
        public static IReadOnlyDictionary<string, PufferNutzungsprofil> Tabelle(
            IEnumerable<(string Quelle, string Schluessel, PufferNutzungsprofil Profil)> eintraege)
        {
            var d = new Dictionary<string, PufferNutzungsprofil>(StringComparer.Ordinal);
            if (eintraege != null)
                foreach (var e in eintraege)
                    d[Schluessel(e.Quelle, e.Schluessel)] = e.Profil;
            return d;
        }

        /// <summary>Die Vorgabe als Nachschlagetabelle (Rückfall ohne Datenbank).</summary>
        public static readonly IReadOnlyDictionary<string, PufferNutzungsprofil> VORGABE_TABELLE = Tabelle(VORGABE);

        /// <summary>Das Profil zu einem Schlüssel; <c>null</c> = nicht zugeordnet.</summary>
        public static PufferNutzungsprofil? Finden(IReadOnlyDictionary<string, PufferNutzungsprofil> tabelle, string quelle, string schluessel)
        {
            if (string.IsNullOrWhiteSpace(schluessel)) return null;
            return (tabelle ?? VORGABE_TABELLE).TryGetValue(Schluessel(quelle, schluessel), out PufferNutzungsprofil p) ? p : null;
        }

        /// <summary>Liest die Zuordnung (Kennung über die ID des Profils).</summary>
        internal const string SQL_LESEN =
            "SELECT z.Quelle, z.Schluessel, p.Kennung FROM " + ProzessNutzungSchema.TAB_ZUORDNUNG + " z INNER JOIN " +
            ProzessNutzungSchema.TAB_PROFIL + " p ON p.ID = z.ID_Nutzungsprofil";

        /// <summary>
        /// <b>Die kleine Leseroutine:</b> die Zuordnung aus <c>Z_Nutzungsprofil</c>; fehlt die Tabelle (vor dem
        /// Schemaschritt) oder ist sie leer, die <see cref="VORGABE"/>. Eine unbekannte Kennung wird übergangen.
        /// </summary>
        public static IReadOnlyDictionary<string, PufferNutzungsprofil> Lesen()
        {
            if (!DataRepository.TabelleVorhanden(ProzessNutzungSchema.TAB_ZUORDNUNG) ||
                !DataRepository.TabelleVorhanden(ProzessNutzungSchema.TAB_PROFIL)) return VORGABE_TABELLE;
            DataTable t = DataRepository.GetDataTable(SQL_LESEN);
            if (t == null || t.Rows.Count == 0) return VORGABE_TABELLE;
            var l = new List<(string, string, PufferNutzungsprofil)>();
            foreach (DataRow r in t.Rows)
                if (Enum.TryParse(Convert.ToString(r["Kennung"]), false, out PufferNutzungsprofil p))
                    l.Add((Convert.ToString(r["Quelle"]), Convert.ToString(r["Schluessel"]), p));
            return Tabelle(l);
        }
    }
}
