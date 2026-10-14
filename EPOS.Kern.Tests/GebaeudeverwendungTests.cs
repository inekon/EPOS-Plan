using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Verwendung beim Import</b> (Anwenderwunsch 08.10.2026, <see cref="Gebaeudeverwendung"/>): Gebäudeart vor
    /// Zonennutzung nach Fläche, ohne Anhaltspunkt die Vorgabe — mit synthetischen Eingaben für gbXML und die Projektdatei.
    /// </summary>
    public sealed class GebaeudeverwendungTests : IDisposable
    {
        private const string WOHN = GebaeudeStammCtrl.FILTERWERT_WOHN;
        private const string NICHT = GebaeudeStammCtrl.FILTERWERT_SONSTIGE;

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _pfade = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string p in _pfade)
                try { if (File.Exists(p)) File.Delete(p); } catch (IOException) { }
        }

        // ------------------------------------------------------------ die Regel

        [Theory]
        [InlineData("Einfamilienhaus", true)]
        [InlineData("kleines Mehrfamilienhaus", true)]
        [InlineData("Reihenmittelhaus", true)]
        [InlineData("Wohnblock", true)]
        [InlineData("SingleFamily", true)]
        [InlineData("MultiFamily", true)]
        [InlineData("Dormitory", true)]
        [InlineData("Verwaltungsgebäude", false)]
        [InlineData("Altenheim", false)]
        [InlineData("Office", false)]
        [InlineData("Warehouse", false)]
        [InlineData("SchoolOrUniversity", false)]
        [InlineData("Unknown", null)]
        [InlineData("NOTDEFINED", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Die_Gebaeudeart_sagt_Wohnen_oder_Nichtwohnen(string art, bool? erwartet)
            => Assert.Equal(erwartet, Gebaeudeverwendung.WohnenAusGebaeudeart(art));

        [Theory]
        [InlineData(1, false)]
        [InlineData(8, false)]
        [InlineData(43, false)]
        [InlineData(70, true)]
        [InlineData(71, true)]
        [InlineData(44, null)]
        [InlineData(null, null)]
        public void Die_DIN_Nummer_der_Projektdatei_sagt_Wohnen_oder_Nichtwohnen(int? nummer, bool? erwartet)
            => Assert.Equal(erwartet, Gebaeudeverwendung.WohnenAusDinNummer(nummer));

        [Theory]
        [InlineData("OfficeEnclosed", false)]
        [InlineData("LivingQuarters", true)]
        [InlineData("Wohnen", true)]
        [InlineData("Corridor", null)]
        [InlineData("Treppenhaus", null)]
        [InlineData("", null)]
        public void Der_Raumtyp_sagt_Wohnen_Nichtwohnen_oder_nichts(string typ, bool? erwartet)
            => Assert.Equal(erwartet, Gebaeudeverwendung.WohnenAusRaumtyp(typ));

        [Fact]
        public void Die_Gebaeudeart_geht_den_Zonen_vor()
        {
            Gebaeudeverwendung.Ergebnis e = Gebaeudeverwendung.Ableiten("Office",
                new[] { new Gebaeudeverwendung.Zonenanteil("Wohnen", true, 500) });
            Assert.Equal(NICHT, e.Verwendung);
            Assert.Equal(Gebaeudeverwendung.Anhalt.Gebaeudeart, e.Anhalt);
            Assert.Equal("Verwendung „Gewerbe+Sonstige“ aus der Gebäudeart „Office“ der Datei.",
                         GebaeudeZuordnungsModell.MeldungText(e.Herleitung));
        }

        [Fact]
        public void Ohne_Gebaeudeart_entscheidet_die_Mehrheit_der_Flaeche_mit_erkannter_Nutzung()
        {
            Gebaeudeverwendung.Ergebnis e = Gebaeudeverwendung.Ableiten("Unknown", new[]
            {
                new Gebaeudeverwendung.Zonenanteil("Büro", false, 300),
                new Gebaeudeverwendung.Zonenanteil("Hausmeisterwohnung", true, 100),
                new Gebaeudeverwendung.Zonenanteil("Flur", null, 400),       // nicht erkannt - zählt nicht
            });
            Assert.Equal(NICHT, e.Verwendung);
            Assert.Equal(Gebaeudeverwendung.Anhalt.Zonennutzung, e.Anhalt);
            Assert.Equal("Verwendung „Gewerbe+Sonstige“ aus den Nutzungen der Zonen: 25 % der 400 m² mit erkannter Nutzung sind Wohnen.",
                         GebaeudeZuordnungsModell.MeldungText(e.Herleitung));

            Assert.Equal(WOHN, Gebaeudeverwendung.Ableiten(null, new[]
            {
                new Gebaeudeverwendung.Zonenanteil("Wohnen", true, 120),
                new Gebaeudeverwendung.Zonenanteil("Praxis", false, 80),
            }).Verwendung);
        }

        [Fact]
        public void Gleichstand_und_kein_Anhaltspunkt_lassen_die_Vorgabe()
        {
            Gebaeudeverwendung.Ergebnis gleich = Gebaeudeverwendung.Ableiten(null, new[]
            {
                new Gebaeudeverwendung.Zonenanteil("A", true, 50), new Gebaeudeverwendung.Zonenanteil("B", false, 50),
            });
            Assert.False(gleich.Abgeleitet);
            Assert.Equal(Gebaeudeverwendung.Anhalt.Keiner, gleich.Anhalt);

            Gebaeudeverwendung.Ergebnis leer = Gebaeudeverwendung.Ableiten("", Array.Empty<Gebaeudeverwendung.Zonenanteil>());
            Assert.Null(leer.Verwendung);
            Assert.Equal("Verwendung: Die Datei nennt weder Gebäudeart noch eindeutige Nutzungen – es bleibt die Vorgabe „Wohngebäude“.",
                         GebaeudeZuordnungsModell.MeldungText(leer.Herleitung));
        }

        // ------------------------------------------------------------ der vorbelegte Satz (Hülle)

        [Fact]
        public void Der_vorbelegte_Satz_traegt_die_Verwendung_ohne_widersprechende_Gebaeudeart()
        {
            var d = new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten { Gebaeudeart = "Einfamilienhaus", Verwendung = WOHN };
            GebaeudeImportHuelle.VerwendungSetzen(d, Gebaeudeverwendung.Ableiten("Office", null));
            Assert.Equal(NICHT, d.Verwendung);
            Assert.Equal(GebaeudeImportHuelle.GEBAEUDEART_SONSTIGE, d.Gebaeudeart);

            // Ohne Anhaltspunkt bleibt der Satz, wie er ist.
            var w = new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten { Gebaeudeart = "Einfamilienhaus", Verwendung = WOHN };
            GebaeudeImportHuelle.VerwendungSetzen(w, Gebaeudeverwendung.Ableiten("", null));
            Assert.Equal(WOHN, w.Verwendung);
            Assert.Equal("Einfamilienhaus", w.Gebaeudeart);
        }

        // ------------------------------------------------------------ gbXML (synthetisch)

        private static string Gbxml(string gebaeudeart, string raeume)
            => Klein.Datei("", raum: raeume).Replace("buildingType=\"SingleFamily\"", "buildingType=\"" + gebaeudeart + "\"");

        private static string Raum(string id, string typ, double flaeche)
            => "<Space id=\"" + id + "\" conditionType=\"Heated\" spaceType=\"" + typ + "\"><Name>" + id + "</Name><Area>"
               + flaeche.ToString(System.Globalization.CultureInfo.InvariantCulture) + "</Area><Volume>100</Volume></Space>";

        [Fact]
        public void GbXML_Office_ist_ein_Nichtwohngebaeude_aus_der_Gebaeudeart()
        {
            GebaeudeImportAblauf a = Klein.Lesen(Gbxml("Office", Raum("r1", "OfficeEnclosed", 120)));
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Equal(NICHT, satz.Verwendung.Verwendung);
            Assert.Equal(Gebaeudeverwendung.Anhalt.Gebaeudeart, satz.Verwendung.Anhalt);
        }

        [Fact]
        public void GbXML_SingleFamily_bleibt_ein_Wohngebaeude()
        {
            GebaeudeImportSatz satz = Klein.Lesen(Gbxml("SingleFamily", Raum("r1", "LivingQuarters", 120))).Zuordnen(0, null);
            Assert.Equal(WOHN, satz.Verwendung.Verwendung);
        }

        [Fact]
        public void GbXML_ohne_Gebaeudeart_folgt_den_Raumtypen_nach_Flaeche()
        {
            string raeume = Raum("r1", "OfficeEnclosed", 200) + Raum("r2", "LivingQuarters", 50) + Raum("r3", "Corridor", 300);
            GebaeudeImportSatz satz = Klein.Lesen(Gbxml("Unknown", raeume)).Zuordnen(0, null);
            Assert.Equal(NICHT, satz.Verwendung.Verwendung);
            Assert.Equal(Gebaeudeverwendung.Anhalt.Zonennutzung, satz.Verwendung.Anhalt);
            Assert.Equal("20", satz.Verwendung.Herleitung.Werte[1]);
            Assert.Equal("250", satz.Verwendung.Herleitung.Werte[2]);
        }

        // ------------------------------------------------------------ Projektdatei (synthetisch)

        private IReadOnlyList<Gebaeudeverwendung.Zonenanteil> Anteile(SqprojProbenErzeuger e, SqprojZonierung zonierung)
        {
            string p = SqprojProbenErzeuger.TempPfad("verwendung");
            _pfade.Add(p);
            SqprojAbbild projekt = SqprojLeser.Lesen(e.Schreiben(p));
            SqprojRaumabgleich abgleich = SqprojRaumabgleich.Bilden(projekt, SqprojProbenErzeuger.IfcAbbild().Gebaeude[0]);
            return SqprojZonen.Verwendungsanteile(projekt, abgleich, zonierung);
        }

        [Fact]
        public void Projektdatei_Buero_50_m2_gegen_Wohnen_40_m2_ist_ein_Nichtwohngebaeude()
        {
            // DIN Büro (A, B; Profil 1, 20 + 30 m²) und DIN Wohnen (C; Profil 71, 40 m²).
            IReadOnlyList<Gebaeudeverwendung.Zonenanteil> zonen = Anteile(SqprojProbenErzeuger.ZweiZonierungen(), SqprojZonierung.Din18599);
            Assert.Equal(2, zonen.Count);
            Assert.Contains(zonen, z => z.Bezeichnung == "DIN Büro" && z.Wohnen == false && Math.Abs(z.FlaecheM2 - 50) < 1e-9);
            Assert.Contains(zonen, z => z.Bezeichnung == "DIN Wohnen" && z.Wohnen == true && Math.Abs(z.FlaecheM2 - 40) < 1e-9);
            Gebaeudeverwendung.Ergebnis e = Gebaeudeverwendung.Ableiten(null, zonen);
            Assert.Equal(NICHT, e.Verwendung);
            Assert.Equal("44", e.Herleitung.Werte[1]);
        }

        [Fact]
        public void Projektdatei_mit_mehr_Wohnflaeche_ist_ein_Wohngebaeude()
        {
            // Nutzung EG (Profil 1, 50 m²) und Nutzung OG (Profil 71, 60 m²).
            IReadOnlyList<Gebaeudeverwendung.Zonenanteil> zonen = Anteile(SqprojProbenErzeuger.Standard(), SqprojZonierung.Din18599);
            Assert.Equal(WOHN, Gebaeudeverwendung.Ableiten(null, zonen).Verwendung);
        }

        [Fact]
        public void Projektdatei_Simulationszone_ohne_Nummer_nimmt_das_Profil_der_Nutzungszone_mit_den_meisten_Raeumen()
        {
            // Dieselbe Wahl der Nummer wie beim Anlegen der Zonen: „Simulation Haus“ (A, B, C) teilt zwei Räume mit „DIN Büro“.
            IReadOnlyList<Gebaeudeverwendung.Zonenanteil> zonen = Anteile(SqprojProbenErzeuger.ZweiZonierungen(), SqprojZonierung.Simulation);
            Gebaeudeverwendung.Zonenanteil z = Assert.Single(zonen);
            Assert.Equal("Simulation Haus", z.Bezeichnung);
            Assert.False(z.Wohnen);
            Assert.Equal(NICHT, Gebaeudeverwendung.Ableiten(null, zonen).Verwendung);
        }
    }
}
