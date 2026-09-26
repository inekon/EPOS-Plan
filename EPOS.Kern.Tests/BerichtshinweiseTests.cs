using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Seiten.Berichte;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die gegliederten Hinweise eines Berichtslaufs: <see cref="BerichtsDaten.Melde"/> schreibt
    /// Fließtext und Liste, <see cref="Berichtshinweise.Gruppiere"/> fasst gleichlautende Hinweise
    /// aller Stände einmal zusammen, die Hülle der Berichtsseite verteilt nach Stufe und betitelt
    /// die Gruppen.
    /// </summary>
    public sealed class BerichtshinweiseTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static VariantenDaten Stand(string name, bool stamm)
            => new VariantenDaten { IstStamm = stamm, Variantenname = name, Projektname = "Musterhaus" };

        private static BerichtsDaten Probe()
        {
            var d = new BerichtsDaten();
            VariantenDaten stamm = Stand("", true), pv = Stand("mit PV", false), sp = Stand("mit Stromspeicher", false);
            d.Varianten.AddRange(new[] { stamm, pv, sp });
            foreach (VariantenDaten v in d.Varianten)
                d.Melde(v, Berichtshinweisstufe.Hinweis, "Zeitbasis Klimadaten: UTC -> MEZ/MESZ");
            d.Melde(pv, Berichtshinweisstufe.Hinweis, "PV-Modul: Nennleistung weicht ab");
            d.Melde(sp, Berichtshinweisstufe.Hinweis, "PV-Modul: Nennleistung weicht ab");
            d.Melde(sp, Berichtshinweisstufe.Warnung, "Kostensätze der Speicherflotte fehlen.");
            d.Melde(stamm, Berichtshinweisstufe.Hinweis, "Wirtschaftlichkeit — CO₂-Pfad | Strombedarf ohne Verwendung",
                    new[] { "Wirtschaftlichkeit — CO₂-Pfad", "Wirtschaftlichkeit — Strombedarf ohne Verwendung" });
            d.Melde(null, Berichtshinweisstufe.Warnung, "Wirtschaftlichkeit: die Rechnung lieferte kein Ergebnis.");
            return d;
        }

        [Fact]
        public void Melde_schreibt_den_Fliesstext_wie_bisher_und_die_Liste_gegliedert()
        {
            BerichtsDaten d = Probe();

            Assert.Contains("Stamm 'Stamm': Zeitbasis Klimadaten: UTC -> MEZ/MESZ", d.Warnungen);
            Assert.Contains("Variante 'mit PV': PV-Modul: Nennleistung weicht ab", d.Warnungen);
            Assert.Contains("Stamm 'Stamm': Wirtschaftlichkeit — CO₂-Pfad | Strombedarf ohne Verwendung", d.Warnungen);
            Assert.Contains("Wirtschaftlichkeit: die Rechnung lieferte kein Ergebnis.", d.Warnungen);

            // Die Teile der Wirtschaftlichkeit sind in der Liste je ein Punkt.
            Assert.Equal(2, d.Hinweisliste.Count(h => h.Stand == "Stamm" && h.Text.StartsWith("Wirtschaftlichkeit")));
            Berichtshinweis w = Assert.Single(d.Hinweisliste, h => h.Stufe == Berichtshinweisstufe.Warnung && h.Stand.Length > 0);
            Assert.Equal("mit Stromspeicher", w.Stand);
            Assert.Equal("Kostensätze der Speicherflotte fehlen.", w.Text);
        }

        [Fact]
        public void Gleichlautende_Hinweise_aller_Staende_stehen_einmal()
        {
            BerichtsDaten d = Probe();
            var staende = d.Varianten.Select(v => v.Anzeige).ToList();
            IReadOnlyList<Berichtshinweisgruppe> g = Berichtshinweise.Gruppiere(
                d.Hinweisliste.Where(h => h.Stufe == Berichtshinweisstufe.Hinweis), staende);

            Assert.Equal(new[] { Berichtshinweisgruppenart.AlleStaende, Berichtshinweisgruppenart.Stand,
                                 Berichtshinweisgruppenart.Stand, Berichtshinweisgruppenart.Stand },
                         g.Select(x => x.Art));
            Assert.Equal(new[] { "Zeitbasis Klimadaten: UTC -> MEZ/MESZ" }, g[0].Texte);
            Assert.Equal("Stamm", g[1].Stand);
            Assert.True(g[1].IstStamm);
            Assert.Equal(2, g[1].Texte.Count);
            // Gleich in ZWEI von drei Ständen ist nicht „alle" — es bleibt je Stand.
            Assert.Equal(new[] { "PV-Modul: Nennleistung weicht ab" }, g[2].Texte);
            Assert.Equal(new[] { "PV-Modul: Nennleistung weicht ab" }, g[3].Texte);
            Assert.DoesNotContain(g.Skip(1), x => x.Texte.Contains("Zeitbasis Klimadaten: UTC -> MEZ/MESZ"));
        }

        [Fact]
        public void Mit_einem_Stand_gibt_es_kein_alle_Staende()
        {
            var d = new BerichtsDaten();
            VariantenDaten stamm = Stand("", true);
            d.Varianten.Add(stamm);
            d.Melde(stamm, Berichtshinweisstufe.Hinweis, "Zeitbasis");
            d.Melde(stamm, Berichtshinweisstufe.Hinweis, "Zeitbasis");   // doppelt — einmal

            var g = Berichtshinweise.Gruppiere(d.Hinweisliste, new[] { "Stamm" });

            Berichtshinweisgruppe einzige = Assert.Single(g);
            Assert.Equal(Berichtshinweisgruppenart.Stand, einzige.Art);
            Assert.Equal(new[] { "Zeitbasis" }, einzige.Texte);
            Assert.Empty(Berichtshinweise.Gruppiere(null, null));
        }

        [Fact]
        public void Die_Huelle_verteilt_nach_Stufe_und_betitelt_die_Gruppen()
        {
            BerichtSeiteGaben.Gliedere(Probe(), null, null, new[] { "Vorlage ohne Deckblatt" }, false,
                                       out IReadOnlyList<Laufhinweisgruppe> warnungen,
                                       out IReadOnlyList<Laufhinweisgruppe> hinweise);

            Assert.Equal(new[] { "Berichtslauf", "Variante „mit Stromspeicher“" }, warnungen.Select(x => x.Titel));
            Assert.Equal("Kostensätze der Speicherflotte fehlen.", warnungen[1].Punkte.Single().Text);

            Assert.Equal(new[] { "Alle Stände", "Stamm", "Variante „mit PV“", "Variante „mit Stromspeicher“",
                                 "Vorprüfung der Vorlage" },
                         hinweise.Select(x => x.Titel));
            Assert.Equal(1, hinweise.SelectMany(x => x.Punkte).Count(p => p.Text.StartsWith("Zeitbasis")));
        }
    }
}
