using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Treffer des Namensabgleichs Schichtsatz → Typgruppe: Gruppencode, das treffende Synonym und die Stufe.</summary>
    internal sealed record Typgruppentreffer(string Gruppe, string Synonym, Abgleichstufe Stufe);

    /// <summary>
    /// <b>Der Namensabgleich Schichtsatz → Typgruppe</b> (Konzept Bauteilaufbau beim Import 5.3 „Namensabgleich auf Gruppen“,
    /// Entwurf Geführter Modellaufbau 2.3, Welle MA-4; Rest von BA-2) — rein, ohne Datenbank. Ein Bauteil, das nur einen U-Wert
    /// und einen Namen bzw. Schichtnamen trägt („Mauerwerk 36,5“, „Brick wall“), wird über eine zweisprachige Synonymtafel der
    /// <b>tragenden Stoffgruppe</b> des Bauteilkatalogs zugeordnet (<see cref="TYPGRUPPEN"/>: Mauerwerk, Beton, Holz, Metalle).
    /// Die Gruppe wählt danach den Katalogaufbau, dessen Schichten dieselbe Gruppe tragen, und beim Typaufbau die Bauart.
    /// <para>Die Stufen folgen dem Muster des Baustoffabgleichs (<see cref="Baustoffabgleich"/>): Der Name wird wie dort
    /// normalisiert (N1/N2: Zahlenschwanz ab, Kleinschreibung, Umlaute aufgelöst, Maße als Marke abgetrennt); dann
    /// <b>N4</b> ein Synonym als ganzes Wort oder ganze Wortfolge, sonst <b>N5</b> ein Synonym ab
    /// <see cref="Baustoffabgleich.TEILWORT_MINDESTLAENGE"/> Zeichen als Wortanfang oder Wortende (deutsche Zusammensetzungen:
    /// „Hochlochziegel“, „Ziegelwand“). Je Stufe gewinnt das längste Synonym („Porenbeton“ vor „Beton“, „Stahlbeton“ vor
    /// „Stahl“); treffen gleich lange Synonyme verschiedene Gruppen, ist der Name mehrdeutig und bleibt ohne Gruppe.</para>
    /// <para>Die Tafel enthält nur Wörter, keine Kennwerte, keine Hersteller- oder Markennamen.</para>
    /// </summary>
    internal static class Aufbaugruppenabgleich
    {
        /// <summary>Die Typgruppen: die tragenden Stoffgruppen des Bauteilkatalogs (Gruppencodes der Baustoffe).</summary>
        internal static readonly IReadOnlyList<string> TYPGRUPPEN = new[] { "MAUERWERK", "BETON", "HOLZ", "METALLE" };

        /// <summary>
        /// Die Synonymtafel (Wort, Sprache, Gruppe) — Wörter in der Form nach N2 (klein, Umlaute aufgelöst). Ein Gegenwort trägt
        /// eine Gruppe außerhalb der Typgruppen: Es gewinnt als längeres Synonym und lässt den Namen ohne Typgruppe.
        /// </summary>
        internal static readonly IReadOnlyList<(string Wort, string Sprache, string Gruppe)> SYNONYME = new[]
        {
            ("mauerwerk", "de", "MAUERWERK"), ("mauer", "de", "MAUERWERK"), ("ziegel", "de", "MAUERWERK"),
            ("backstein", "de", "MAUERWERK"), ("klinker", "de", "MAUERWERK"), ("kalksandstein", "de", "MAUERWERK"),
            ("ks", "de", "MAUERWERK"), ("porenbeton", "de", "MAUERWERK"), ("gasbeton", "de", "MAUERWERK"),
            ("leichtbetonstein", "de", "MAUERWERK"), ("bims", "de", "MAUERWERK"), ("naturstein", "de", "MAUERWERK"),
            ("bruchstein", "de", "MAUERWERK"), ("sandstein", "de", "MAUERWERK"), ("lehmstein", "de", "MAUERWERK"),
            ("masonry", "en", "MAUERWERK"), ("brick", "en", "MAUERWERK"), ("bricks", "en", "MAUERWERK"),
            ("brickwork", "en", "MAUERWERK"), ("blockwork", "en", "MAUERWERK"),
            ("stone", "en", "MAUERWERK"), ("sand lime", "en", "MAUERWERK"), ("calcium silicate", "en", "MAUERWERK"),
            ("aerated concrete", "en", "MAUERWERK"), ("concrete block", "en", "MAUERWERK"), ("clay brick", "en", "MAUERWERK"),

            ("beton", "de", "BETON"), ("stahlbeton", "de", "BETON"), ("stb", "de", "BETON"), ("ortbeton", "de", "BETON"),
            ("leichtbeton", "de", "BETON"), ("betonfertigteil", "de", "BETON"),
            ("concrete", "en", "BETON"), ("reinforced concrete", "en", "BETON"), ("precast concrete", "en", "BETON"),

            ("holz", "de", "HOLZ"), ("holzstaender", "de", "HOLZ"), ("holzrahmen", "de", "HOLZ"), ("holztafel", "de", "HOLZ"),
            ("holzbau", "de", "HOLZ"), ("holzbalken", "de", "HOLZ"), ("fachwerk", "de", "HOLZ"), ("brettsperrholz", "de", "HOLZ"),
            ("massivholz", "de", "HOLZ"), ("sparren", "de", "HOLZ"),
            ("timber", "en", "HOLZ"), ("wood", "en", "HOLZ"), ("wooden", "en", "HOLZ"), ("timber frame", "en", "HOLZ"),
            ("cross laminated timber", "en", "HOLZ"), ("clt", "en", "HOLZ"), ("rafter", "en", "HOLZ"),

            ("stahl", "de", "METALLE"), ("metall", "de", "METALLE"), ("trapezblech", "de", "METALLE"),
            ("sandwichpaneel", "de", "METALLE"), ("aluminium", "de", "METALLE"),
            ("steel", "en", "METALLE"), ("metal", "en", "METALLE"), ("sandwich panel", "en", "METALLE"),

            // Gegenwörter: Dämmstoffe, deren Name ein Wort einer Typgruppe trägt — sie führen auf keine Typgruppe.
            ("holzfaser", "de", "DAEMMSTOFFE"), ("holzwolle", "de", "DAEMMSTOFFE"), ("steinwolle", "de", "DAEMMSTOFFE"),
            ("wood fibre", "en", "DAEMMSTOFFE"), ("wood fiber", "en", "DAEMMSTOFFE"), ("wood wool", "en", "DAEMMSTOFFE"),
            ("stone wool", "en", "DAEMMSTOFFE"), ("rock wool", "en", "DAEMMSTOFFE"),
        };

        /// <summary>Ist der Gruppencode eine Typgruppe?</summary>
        internal static bool IstTypgruppe(string gruppe) => gruppe != null && TYPGRUPPEN.Contains(gruppe, StringComparer.Ordinal);

        /// <summary>
        /// Die Typgruppe eines Namens (Bauteilname, Aufbauname oder Schichtname); <c>null</c> ohne Treffer oder mehrdeutig.
        /// </summary>
        internal static Typgruppentreffer Abgleichen(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string kern = Baustoffabgleich.Normalisieren(name).Kern;
            if (string.IsNullOrWhiteSpace(kern)) return null;
            string[] woerter = kern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string umrahmt = " " + string.Join(" ", woerter) + " ";

            // N4: Synonym als ganzes Wort bzw. ganze Wortfolge.
            (Typgruppentreffer t, bool mehrdeutig) = Bester(SYNONYME.Where(s => umrahmt.Contains(" " + s.Wort + " ", StringComparison.Ordinal)),
                                                            Abgleichstufe.Synonym);
            if (t == null && !mehrdeutig)
            {
                // N5: Synonym (ein Wort, ab Mindestlänge) als Wortanfang oder Wortende.
                (t, mehrdeutig) = Bester(SYNONYME.Where(s => s.Wort.IndexOf(' ') < 0 && s.Wort.Length >= Baustoffabgleich.TEILWORT_MINDESTLAENGE
                                                             && woerter.Any(w => w.Length > s.Wort.Length
                                                                                 && (w.StartsWith(s.Wort, StringComparison.Ordinal)
                                                                                     || w.EndsWith(s.Wort, StringComparison.Ordinal)))),
                                         Abgleichstufe.Teilwort);
            }
            return t != null && IstTypgruppe(t.Gruppe) ? t : null;
        }

        /// <summary>Das längste Synonym; gleich lange Synonyme verschiedener Gruppen → mehrdeutig.</summary>
        private static (Typgruppentreffer Treffer, bool Mehrdeutig) Bester(IEnumerable<(string Wort, string Sprache, string Gruppe)> treffer,
                                                                          Abgleichstufe stufe)
        {
            var liste = treffer.ToList();
            if (liste.Count == 0) return (null, false);
            int laenge = liste.Max(x => x.Wort.Length);
            var laengste = liste.Where(x => x.Wort.Length == laenge).ToList();
            if (laengste.Select(x => x.Gruppe).Distinct(StringComparer.Ordinal).Count() > 1) return (null, true);
            return (new Typgruppentreffer(laengste[0].Gruppe, laengste[0].Wort, stufe), false);
        }

        /// <summary>
        /// Die Typgruppe eines Schichtsatzes: die Gruppe, die die meisten Schichtnamen treffen; bei Gleichstand verschiedener
        /// Gruppen <c>null</c>.
        /// </summary>
        internal static Typgruppentreffer Abgleichen(IEnumerable<string> schichtnamen)
        {
            var treffer = (schichtnamen ?? Enumerable.Empty<string>()).Select(Abgleichen).Where(x => x != null).ToList();
            if (treffer.Count == 0) return null;
            var jeGruppe = treffer.GroupBy(x => x.Gruppe, StringComparer.Ordinal).OrderByDescending(g => g.Count()).ToList();
            if (jeGruppe.Count > 1 && jeGruppe[0].Count() == jeGruppe[1].Count()) return null;
            return jeGruppe[0].First();
        }

        /// <summary>
        /// Die Typgruppen eines Aufbaus: die Typgruppen der Stoffgruppen seiner Schichten und die seines Namens.
        /// </summary>
        internal static IReadOnlyCollection<string> Typgruppen(string bezeichner, IEnumerable<string> stoffgruppen)
        {
            var g = new HashSet<string>(StringComparer.Ordinal);
            foreach (string s in stoffgruppen ?? Enumerable.Empty<string>())
                if (IstTypgruppe(s)) g.Add(s);
            if (Abgleichen(bezeichner) is Typgruppentreffer t) g.Add(t.Gruppe);
            return g;
        }
    }
}
