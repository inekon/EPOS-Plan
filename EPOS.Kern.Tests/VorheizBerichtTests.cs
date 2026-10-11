using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Vorheizen der Fassung 2 in Bericht, Kennzahlen, Export und Variantenvergleich</b> (AH-V6; Entwurf Vorheizrampe
    /// 2.9): die Vorheizzeilen der Gebäudetafel je Verfahren mit Einheiten und deutschem Zahlformat, die Tafel „Vorheizen je
    /// Zone“, die Vorlagenfelder der Fassung 19, die Gebäudekennzahlen mit Δ, die Exportschlüssel <c>Geb[n].Vorheiz*</c> und
    /// die Abweichungsmerkmale — und bei Sollwertrampe nichts davon.
    /// </summary>
    [Collection("Testdatenbank")]
    public class VorheizBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        public void Dispose() => _kultur.Dispose();

        private static Vorheizkennzahlen Vorheizen(string verfahren, int zeit = 6, double leistung = 12.34, double mehrwaerme = 1.234)
            => new Vorheizkennzahlen
            {
                Verfahren = verfahren, VorheizzeitH = zeit, VorheizzeitBedarfMaxH = 9, BezugKw = 40.0, DeckelKw = 32.5,
                LeistungKw = leistung, SprungVorlaufMaxKw = 18.26, SprungLaufMaxKw = 22.0, TageVerfehlt = 3,
                TageUeberVorheizzeit = verfahren == VorheizenSchema.VERFAHREN_BERECHNET ? 7 : (int?)null,
                UnterschreitungMaxK = 0.44, UnterschreitungH = 12, DeckelstundenH = 1234, FloorstundenH = 56,
                NaechteOhneAbsenkung = 4, UnerreichbarTage = 2, SperrzeitTage = 5, MehrwaermeMwh = mehrwaerme,
            };

        private static ErgebnisGebaeudeModel Gebaeude(int id, Vorheizkennzahlen v, params ErgebnisZoneModel[] zonen) => new ErgebnisGebaeudeModel
        {
            ID_Gebaeude = id, Merkplatz = id, Gebaeudename = "Gebäude " + id, Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
            HeizwaermeMwh = 100.0, SpitzeKw = 70.0, SpitzeTagesmittelKw = 30.0, Spitze95Kw = 40.0,
            AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizArt = DbWerte.AUFHEIZ_ART_TAEGLICH, AufheizzeitMaxH = 6,
            Vorheizen = v, Zonen = zonen.ToList(),
        };

        private static Dictionary<string, string> Tafel(ErgebnisGebaeudeModel g)
            => Aufheizbericht.Zeilen(g, null, DE).ToDictionary(z => z.Beschriftung, z => z.Wert.Text);

        // =================================================================
        // Gebäudetafel
        // =================================================================

        [Fact]
        public void Vorgabe_zeigt_die_Vorheizgroessen_mit_Einheit_und_deutschem_Zahlformat()
        {
            Dictionary<string, string> t = Tafel(Gebaeude(1, Vorheizen(VorheizenSchema.VERFAHREN_VORGABE)));
            Assert.Equal(R.BV_VORH_VERFAHREN_VORGABE, t[R.BV_VORH_VERFAHREN]);
            Assert.Equal("6 h", t[R.BV_VORH_ZEIT_VORGABE]);
            Assert.False(t.ContainsKey(R.BV_VORH_ZEIT_BERECHNET));
            Assert.Equal("9 h", t[R.BV_VORH_BEDARF_MAX]);
            Assert.Equal("40,0 kW", t[R.BV_VORH_BEZUG]);
            Assert.Equal("32,5 kW", t[R.BV_VORH_DECKEL]);
            Assert.Equal("12,3 kW", t[R.BV_VORH_LEISTUNG]);
            Assert.Equal("18,3 kW", t[R.BV_VORH_SPRUNG_VORLAUF]);
            Assert.Equal("22,0 kW", t[R.BV_VORH_SPRUNG_LAUF]);
            Assert.Equal("3", t[R.BV_VORH_TAGE_VERFEHLT]);
            Assert.False(t.ContainsKey(R.BV_VORH_TAGE_UEBER));
            Assert.Equal("0,4 K", t[R.BV_VORH_UNTERSCHREITUNG_MAX]);
            Assert.Equal("12 h/a", t[R.BV_VORH_UNTERSCHREITUNG_H]);
            Assert.Equal("1.234 h/a", t[R.BV_VORH_DECKELSTUNDEN]);
            Assert.Equal("56 h/a", t[R.BV_VORH_FLOORSTUNDEN]);
            Assert.Equal("4", t[R.BV_VORH_NAECHTE]);
            Assert.Equal("2", t[R.BV_VORH_UNERREICHBAR]);
            Assert.Equal("5", t[R.BV_VORH_SPERRZEIT]);
            Assert.Equal("1,23 MWh/a", t[R.BV_VORH_MEHRWAERME]);
        }

        [Fact]
        public void Berechnet_nennt_das_Quantil_und_die_Tage_ueber_der_Vorheizzeit()
        {
            Dictionary<string, string> t = Tafel(Gebaeude(1, Vorheizen(VorheizenSchema.VERFAHREN_BERECHNET, zeit: 11)));
            Assert.Equal(R.BV_VORH_VERFAHREN_BERECHNET, t[R.BV_VORH_VERFAHREN]);
            Assert.Equal("11 h", t[R.BV_VORH_ZEIT_BERECHNET]);
            Assert.False(t.ContainsKey(R.BV_VORH_ZEIT_VORGABE));
            Assert.Equal("7", t[R.BV_VORH_TAGE_UEBER]);
        }

        [Fact]
        public void Sollwertrampe_laesst_Tafel_Zonentafel_und_Schalter_wie_sie_waren()
        {
            ErgebnisGebaeudeModel mit = Gebaeude(1, Vorheizen(VorheizenSchema.VERFAHREN_VORGABE));
            ErgebnisGebaeudeModel ohne = Gebaeude(1, null, new ErgebnisZoneModel { Bezeichner = "Nord", Rang = 1 });
            List<(string Beschriftung, Tabellenzelle Wert)> zeilen = Aufheizbericht.Zeilen(ohne, null, DE).ToList();
            Assert.DoesNotContain(zeilen, z => z.Beschriftung == R.BV_VORH_VERFAHREN);
            // Die Rampenzeilen sind dieselben wie mit Vorheizen — nur die Vorheizzeilen kommen hinzu.
            Assert.Equal(zeilen.Select(z => z.Beschriftung + "=" + z.Wert.Text),
                         Aufheizbericht.Zeilen(mit, null, DE).Where(z => !Vorheizbeschriftung(z.Beschriftung))
                                       .Select(z => z.Beschriftung + "=" + z.Wert.Text));
            Assert.Null(Aufheizbericht.Zonentafel(ohne, DE));
            Assert.Empty(Aufheizbericht.Vorheizzeilen(null, DE));
            Assert.Equal("", Aufheizbericht.Verfahrenstext(null, DE));
        }

        private static bool Vorheizbeschriftung(string b)
            => typeof(R).GetProperties().Where(p => p.Name.StartsWith("BV_VORH_", StringComparison.Ordinal))
                        .Any(p => (string)p.GetValue(null) == b);

        [Fact]
        public void Die_Zonentafel_fuehrt_je_Zone_mit_Vorheizen_eine_Zeile()
        {
            ErgebnisGebaeudeModel g = Gebaeude(1, Vorheizen(VorheizenSchema.VERFAHREN_VORGABE),
                new ErgebnisZoneModel { Rang = 1, Bezeichner = "Büro", Vorheizen = Vorheizen(VorheizenSchema.VERFAHREN_VORGABE, 5, 8.26, 0.5) },
                new ErgebnisZoneModel { Rang = 2, Bezeichner = "Lager" },
                new ErgebnisZoneModel { Rang = 3, Bezeichner = "Halle", Vorheizen = Vorheizen(VorheizenSchema.VERFAHREN_VORGABE, 7, 4.0, 0.126) });
            Berichtstabelle t = Aufheizbericht.Zonentafel(g, DE);
            Assert.NotNull(t);
            Assert.Equal(2, t.Zeilen.Count);
            Assert.Equal(new[] { "Büro", "5", "8,3", "3", "0,4", "0,50" }, t.Zeilen[0].Zellen.Select(c => c.Text).ToArray());
            Assert.Equal(new[] { "Halle", "7", "4,0", "3", "0,4", "0,13" }, t.Zeilen[1].Zellen.Select(c => c.Text).ToArray());
            Assert.Equal(R.BV_VORH_ZONEN_TITEL, Aufheizbericht.Zonentitel(DE));
        }

        // =================================================================
        // Vorlagenfelder (Katalog v19)
        // =================================================================

        [Fact]
        public void Die_Vorlagenfelder_stehen_ab_Fassung_19_mit_Format_und_Einheit()
        {
            Assert.Equal(19, Vorlagenfeldkatalog.FASSUNG_VORHEIZEN);
            Assert.True(Vorlagenfeldkatalog.KATALOGFASSUNG >= 19);
            (string Schluessel, string Format, string Einheit)[] erwartet =
            {
                ("gebaeude.ergebnis.vorheizzeit", "N0", "h"),
                ("gebaeude.ergebnis.vorheizleistung", "N1", "kW"),
                ("gebaeude.ergebnis.vorheiz_bezug", "N1", "kW"),
                ("gebaeude.ergebnis.vorheiz_unterschreitung_max", "N1", "K"),
                ("gebaeude.ergebnis.vorheiz_deckelstunden", "N0", "h/a"),
                ("gebaeude.ergebnis.vorheiz_mehrwaerme", "N2", "MWh/a"),
            };
            foreach ((string s, string f, string e) in erwartet)
            {
                Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(s);
                Assert.True(feld != null, s);
                Assert.Equal(19, feld.Seit);
                Assert.Equal(f, feld.Format);
                Assert.Equal(e, feld.Einheit);
            }
            Assert.Equal(19, Vorlagenfeldkatalog.Finde("gebaeude.ergebnis.vorheizverfahren").Seit);
            Assert.Equal(19, Vorlagenfeldkatalog.Finde("hat.vorheizen").Seit);
            Assert.Equal(18, Vorlagenfeldkatalog.Alle.Count(f => f.Schluessel.StartsWith("gebaeude.ergebnis.vorheiz", StringComparison.Ordinal)));
        }

        // =================================================================
        // Kennzahlen mit Δ und Variantenvergleich
        // =================================================================

        [Fact]
        public void Die_Gebaeudekennzahlen_tragen_Hoechstwerte_und_Summen_und_entfallen_bei_Sollwertrampe()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            daten.Varianten[0].Ergebnis.Gebaeude.AddRange(new[]
            {
                Gebaeude(1, Vorheizen(VorheizenSchema.VERFAHREN_VORGABE, 6, 10.0, 1.5)),
                Gebaeude(2, Vorheizen(VorheizenSchema.VERFAHREN_VORGABE, 9, 5.0, 0.5)),
            });
            daten.Varianten[1].Ergebnis.Gebaeude.Add(Gebaeude(1, null));
            foreach (VariantenDaten v in daten.Varianten) KennzahlenKatalog.Berechne(v);

            VariantenDaten stamm = daten.Varianten[0];
            Assert.Equal(9.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZZEIT]);
            Assert.Equal(15.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZLEISTUNG]);
            Assert.Equal(3.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZ_TAGE_VERFEHLT]);
            Assert.Equal(2.0, stamm.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZ_MEHRWAERME]);

            VariantenDaten rampe = daten.Varianten[1];
            Assert.False(rampe.Kennzahlen.TryGetValue(KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZZEIT, out double? z) && z.HasValue);
            Assert.False(rampe.Kennzahlen.TryGetValue(KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZ_MEHRWAERME, out double? m) && m.HasValue);
        }

        private static ProjektDetails Einstellung(string verfahren, long? zeit, string geltung, double? genauigkeit)
        {
            var ein = new DataTable();
            ein.Columns.Add("ID", typeof(long));
            ein.Columns.Add("ID_Projekt", typeof(long));
            ein.Columns.Add(VorheizenSchema.SPALTE_VERFAHREN, typeof(string));
            ein.Columns.Add(VorheizenSchema.SPALTE_VORHEIZZEIT, typeof(long));
            ein.Columns.Add(VorheizenSchema.SPALTE_GELTUNG, typeof(string));
            ein.Columns.Add(VorheizenSchema.SPALTE_REGELGENAUIGKEIT, typeof(double));
            ein.Rows.Add(1L, 1L, (object)verfahren ?? DBNull.Value, (object)zeit ?? DBNull.Value, (object)geltung ?? DBNull.Value,
                         (object)genauigkeit ?? DBNull.Value);
            return new ProjektDetails { IdProjekt = 1, Einstellungen = ein };
        }

        [Fact]
        public void Der_Variantenvergleich_meldet_Verfahren_Vorheizzeit_und_Geltung_NULL_ist_die_Vorgabe()
        {
            Assert.Empty(AbweichungsErmittler.Vergleiche(Einstellung(null, null, null, null),
                                                         Einstellung(VorheizenSchema.VERFAHREN_RAMPE, null, VorheizenSchema.GELTUNG_GEBAEUDE, 1.0)));

            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(Einstellung(null, null, null, null),
                Einstellung(VorheizenSchema.VERFAHREN_BERECHNET, 8, VorheizenSchema.GELTUNG_ZONE, 0.5));
            Abweichung Finde(string merkmal) => liste.Single(a => a.Gewerk == "Gebäude" && a.Merkmal == merkmal);
            Assert.Equal(R.ABW_MERKMAL_VORH_WERT_RAMPE, Finde(R.ABW_MERKMAL_VORH_VERFAHREN).WertStamm);
            Assert.Equal(R.BV_VORH_VERFAHREN_BERECHNET, Finde(R.ABW_MERKMAL_VORH_VERFAHREN).WertVariante);
            Assert.Equal("8 h", Finde(R.ABW_MERKMAL_VORH_ZEIT).WertVariante);
            Assert.Equal(R.ABW_MERKMAL_VORH_WERT_ZONE, Finde(R.ABW_MERKMAL_VORH_GELTUNG).WertVariante);
            Assert.Equal("1,0 K", Finde(R.ABW_MERKMAL_VORH_REGELGENAUIGKEIT).WertStamm);
            Assert.Equal("0,5 K", Finde(R.ABW_MERKMAL_VORH_REGELGENAUIGKEIT).WertVariante);
            Assert.Equal(4, liste.Count);
        }

        // =================================================================
        // Export Geb[n].Vorheiz*
        // =================================================================

        [Fact]
        public void Die_Exportschluessel_folgen_den_Ergebnisspalten_und_lassen_leere_Werte_weg()
        {
            Vorheizkennzahlen v = Vorheizen(VorheizenSchema.VERFAHREN_VORGABE);
            List<(string Schluessel, double? Wert)> s = GebaeudeErgebnisexport.Vorheizschluessel(v).ToList();
            Assert.Equal(17, s.Count);
            Assert.All(s, x => Assert.StartsWith("Vorheiz", x.Schluessel));
            Assert.Equal(s.Count, s.Select(x => x.Schluessel).Distinct().Count());
            Assert.Equal(("VorheizzeitH", (double?)6), s[0]);
            Assert.Equal(("VorheizMehrwaermeMwh", (double?)1.234), s[16]);
            Assert.Null(s.Single(x => x.Schluessel == "VorheizTageUeberVorheizzeit").Wert);
        }
    }
}
