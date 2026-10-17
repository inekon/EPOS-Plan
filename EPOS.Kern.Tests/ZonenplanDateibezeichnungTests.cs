using System;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Anwenderentscheid F2 auf den bestehenden Zonenpfaden</b> (Entwurf ZZ, Abschnitt 10): Trägt eine Zone ihr Profil aus
    /// der Bezeichnung der Datei — <c>EPOS_Zone.DinNummer</c> einer IFC-Datei, die DIN-Nummer einer gbXML-Datei —, bleibt das
    /// Profil vorbelegt (der Rechenweg ändert sich nicht), die Quelle ist aber ein Vorschlag mit Herkunft „Bezeichnung der
    /// Datei“, bis „Alle Vorschläge übernehmen“ ihn bestätigt; „Zurücknehmen“ stellt den Vorschlag wieder her.
    /// </summary>
    public sealed class ZonenplanDateibezeichnungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeAbbild Abbild(string format)
        {
            AbbildRaum R(string kennung, string name, double m2, bool beheizt, int? din)
                => new AbbildRaum
                {
                    Kennung = kennung, Name = name, FlaecheM2 = m2, Beheizt = beheizt, GeschossKennung = "EG", GeschossName = "EG",
                    Konditionierung = din is int n ? new AbbildKonditionierung { DinNummer = n } : null,
                };
            var a = new GebaeudeAbbild { Format = format };
            var g = new AbbildGebaeude { Kennung = "G", Name = "Gebäude" };
            g.Raeume.Add(R("b1", "Besprechung 1", 20, true, 4));
            g.Raeume.Add(R("b2", "Besprechung 2", 25, true, 4));
            g.Raeume.Add(R("l1", "Lager", 30, false, null));
            a.Gebaeude.Add(g);
            return a;
        }

        private static Planzone ZoneVon(Zonenplan plan, string raum) => plan.Zone(plan.ZoneVon(raum));

        [Theory]
        [InlineData(GebaeudeQuelle.FORMAT_IFC, "Z6")]
        [InlineData(GebaeudeQuelle.FORMAT_GBXML, null)]
        public void DIN_Nummer_der_Datei_ist_ein_Vorschlag_bis_zur_Bestaetigung(string format, string regel)
        {
            Zonenplan plan = Zonenplan.Vorschlag(Abbild(format), 0, regel, vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            Assert.NotNull(plan);
            Assert.Null(plan.Ablehnung);
            Planzone z = ZoneVon(plan, "b1");
            Assert.NotNull(z.Profil);
            string profil = z.Profil.Name;
            Assert.Equal(Raumprofilzuordnung.QUELLE_VORSCHLAG, z.Quelle.Art);
            Assert.Contains("DIN 4", z.Quelle.Schluessel);
            Assert.Equal("4", z.Dateivorschlag);
            if (format == GebaeudeQuelle.FORMAT_GBXML) Assert.Empty(plan.Raumprofile);

            Assert.True(plan.VorschlaegeUebernehmen().Ok);
            z = ZoneVon(plan, "b1");
            Assert.Equal(profil, z.Profil.Name);   // dasselbe Profil, nur bestätigt
            Assert.Equal(Raumprofilzuordnung.QUELLE, z.Quelle.Art);
            Assert.Null(z.Dateivorschlag);
            Assert.Equal("4", plan.Profilwahl["b1"]);
            Assert.False(plan.Profilwahl.ContainsKey("l1"));

            Assert.True(plan.VorschlaegeZuruecknehmen().Ok);
            z = ZoneVon(plan, "b1");
            Assert.Equal(profil, z.Profil.Name);
            Assert.Equal(Raumprofilzuordnung.QUELLE_VORSCHLAG, z.Quelle.Art);
            Assert.Equal("4", z.Dateivorschlag);
        }

        [Fact]
        public void Eine_DIN_Nummer_ohne_Profil_behaelt_ihre_Quelle()
        {
            GebaeudeAbbild a = Abbild(GebaeudeQuelle.FORMAT_IFC);
            foreach (AbbildRaum r in a.Gebaeude[0].Raeume.Where(r => r.Konditionierung != null)) r.Konditionierung.DinNummer = 99;
            Zonenplan plan = Zonenplan.Vorschlag(a, 0, "Z6", vorbelegung: Raumnutzungsvorbelegung.Vorgabe);
            Planzone z = ZoneVon(plan, "b1");
            Assert.Null(z.Profil);
            Assert.Equal(RaumnutzungSchema.ZUORDNUNG_DIN, z.Quelle.Art);
            Assert.Equal("99", z.Quelle.Schluessel);
            Assert.Null(z.Dateivorschlag);
        }
    }
}
