using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die spezifische Wärmekapazität einer Schicht, wenn die Datei sie nicht führt</b> (Mehrzonenkonzept 6.5):
    /// IFC4-Exporte schreiben oft Dichte und Wärmeleitfähigkeit, aber keine <c>SpecificHeatCapacity</c> — die Schicht
    /// wäre sonst masselos. Rückfall in zwei Stufen, ohne Datenbank: zuerst der Namensabgleich gegen den
    /// Baustoffkatalog der Auslieferung (<see cref="Baustoffabgleich"/>, c des getroffenen Baustoffs im Band), sonst
    /// eine kleine benannte Stofftabelle nach Stoffgruppen (<see cref="TAFEL"/>). Ohne Treffer bleibt die Schicht ohne
    /// c. Gilt nur für c — Dichte und Wärmeleitfähigkeit kommen immer aus der Datei.
    /// </summary>
    internal static class Waermekapazitaetsrueckfall
    {
        /// <summary>Eine Stoffgruppe der Tafel: Name, c [J/(kg·K)], Schlüsselwörter (normalisiert, Teilwort) und kurze Wörter (nur ganz).</summary>
        internal sealed record Stoffgruppe(string Name, double CpJkgK, string[] Teilwoerter, string[] GanzeWoerter);

        /// <summary>
        /// <b>Die Stofftabelle</b> — c nach DIN EN ISO 10456:2010, Tabelle 3 (Baustoffe) und Tabelle 4 (Dämmstoffe). Die
        /// Reihenfolge entscheidet: Dämmstoffe vor den mineralischen Gruppen (eine „Leichtbauplatte mit Mineralfaser" ist
        /// Dämmstoff), Gips vor Putz, Beton zuletzt.
        /// </summary>
        internal static readonly IReadOnlyList<Stoffgruppe> TAFEL = new[]
        {
            // ISO 10456 Tab. 4: Polystyrol expandiert und extrudiert 1450, Polyurethan 1400, Phenolharz 1400 — Gruppe 1450.
            new Stoffgruppe("Dämmstoff (Schaumkunststoff)", 1450.0,
                new[] { "polystyrol", "styropor", "styrodur", "extruder", "hartschaum", "polyurethan", "phenolharz" },
                new[] { "eps", "xps", "pur", "pir", "ps" }),
            // ISO 10456 Tab. 4: Mineralwolle 1030.
            new Stoffgruppe("Dämmstoff (Mineralfaser)", 1030.0,
                new[] { "mineralwolle", "mineralfaser", "steinwolle", "glaswolle", "faserdaemmstoff", "daemmstoff" },
                Array.Empty<string>()),
            // ISO 10456 Tab. 3: Bauholz 1600.
            new Stoffgruppe("Holz", 1600.0, new[] { "holz", "timber", "wood" }, Array.Empty<string>()),
            // ISO 10456 Tab. 3: Gips, Gipskartonplatte 1000.
            new Stoffgruppe("Gips", 1000.0, new[] { "gips", "gypsum" }, Array.Empty<string>()),
            // ISO 10456 Tab. 3: Putz und Mörtel 1000; Zementestrich wie Mörtel.
            new Stoffgruppe("Putz", 1000.0, new[] { "putz", "moertel", "estrich", "plaster", "mortar" }, Array.Empty<string>()),
            // ISO 10456 Tab. 3: Mauerwerk (Ziegel, Kalksandstein, Porenbeton) 1000.
            new Stoffgruppe("Mauerwerk", 1000.0,
                new[] { "ziegel", "kalksandstein", "mauerwerk", "klinker", "porenbeton", "masonry", "brick" }, Array.Empty<string>()),
            // ISO 10456 Tab. 3: Beton (Normal-, Leicht-, Stahlbeton) 1000.
            new Stoffgruppe("Beton", 1000.0, new[] { "beton", "concrete" }, Array.Empty<string>()),
        };

        /// <summary>Die Quelle eines Tafelwerts im Protokoll.</summary>
        internal const string QUELLE_TAFEL = "DIN EN ISO 10456";

        /// <summary>
        /// c [J/(kg·K)] für einen Materialnamen und die Quelle für das Protokoll (Katalogbezeichner bzw.
        /// „DIN EN ISO 10456 (Gruppe)"); <c>null</c> = kein Treffer. <paramref name="abgleich"/> darf fehlen.
        /// </summary>
        internal static (double CpJkgK, string Quelle)? Bestimmen(string materialname, Baustoffabgleich abgleich)
        {
            if (string.IsNullOrWhiteSpace(materialname)) return null;
            Abgleichtreffer t = abgleich?.Abgleichen(materialname);
            if (t != null && t.Getroffen && Baustoffabgleich.CpImBand(t.Baustoff.Cp))
                return (t.Baustoff.Cp.Value, t.Baustoff.Bezeichner);
            Stoffgruppe g = Gruppe(materialname);
            return g == null ? null : (g.CpJkgK, QUELLE_TAFEL + " (" + g.Name + ")");
        }

        /// <summary>Die Stoffgruppe der Tafel zu einem Materialnamen; <c>null</c> = keine.</summary>
        internal static Stoffgruppe Gruppe(string materialname)
        {
            string[] woerter = Baustoffabgleich.Schluessel(materialname ?? "")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (Stoffgruppe g in TAFEL)
                if (woerter.Any(w => g.Teilwoerter.Any(t => w.Contains(t, StringComparison.Ordinal))
                                     || g.GanzeWoerter.Any(t => string.Equals(w, t, StringComparison.Ordinal))))
                    return g;
            return null;
        }
    }
}
