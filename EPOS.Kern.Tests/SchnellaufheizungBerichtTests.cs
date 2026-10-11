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
    /// <b>Die Schnellaufheizung in Bericht, Kennzahlen, Export und Variantenvergleich</b> (SA3b, Schritt 217): die sechs Zeilen
    /// der Gebäudetafel mit Einheiten und deutschem Zahlformat, die zwei Spalten der Tafel „Vorheizen je Zone“, die Vorlagenfelder
    /// der Fassung 21 samt Halbsatz des Kurzberichts, die Kennzahl der Stunden mit Überhöhung, die Exportschlüssel und die
    /// Abweichungsmerkmale — und ohne Wirkung der Überhöhung (Vorheizen ohne Schalter, Sollwertrampe) nichts davon.
    /// </summary>
    [Collection("Testdatenbank")]
    public class SchnellaufheizungBerichtTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        public void Dispose() => _kultur.Dispose();

        private static Vorheizkennzahlen Vorheizen(bool schnell, double ueberhoehung = 2.0, int stunden = 345)
        {
            var v = new Vorheizkennzahlen
            {
                Verfahren = VorheizenSchema.VERFAHREN_VORGABE, VorheizzeitH = 6, LeistungKw = 12.34, TageVerfehlt = 3,
                UnterschreitungMaxK = 0.44, MehrwaermeMwh = 1.234,
            };
            if (schnell)
            {
                v.UeberhoehungK = ueberhoehung; v.UeberhoehungH = stunden; v.UeberhoehungVorlaufMaxC = 58.26;
                v.VorlaufgrenzeH = 12; v.UeberhoehungWirkungslosH = 0; v.UeberhoehungSperrzeitH = 1234;
            }
            return v;
        }

        private static ErgebnisGebaeudeModel Gebaeude(int id, Vorheizkennzahlen v, params ErgebnisZoneModel[] zonen) => new ErgebnisGebaeudeModel
        {
            ID_Gebaeude = id, Merkplatz = id, Gebaeudename = "Gebäude " + id, Rechenweg = DbWerte.GEBAEUDE_MODELL_VDI6007,
            HeizwaermeMwh = 100.0, SpitzeKw = 70.0, SpitzeTagesmittelKw = 30.0, Spitze95Kw = 40.0,
            Vorheizen = v, Zonen = zonen.ToList(),
        };

        private static string[] Tafel(ErgebnisGebaeudeModel g)
            => Aufheizbericht.Zeilen(g, null, DE).Select(z => z.Beschriftung + "=" + z.Wert.Text).ToArray();

        // =================================================================
        // Gebäudetafel und Tafel „Vorheizen je Zone“
        // =================================================================

        [Fact]
        public void Mit_Schnellaufheizung_zeigt_die_Tafel_sechs_Zeilen_mit_Einheit_und_deutschem_Zahlformat()
        {
            Dictionary<string, string> t = Aufheizbericht.Zeilen(Gebaeude(1, Vorheizen(true)), null, DE)
                                                          .ToDictionary(z => z.Beschriftung, z => z.Wert.Text);
            Assert.Equal("2,0 K", t[R.BV_VORH_SA_UEBERHOEHUNG]);
            Assert.Equal("345 h/a", t[R.BV_VORH_SA_STUNDEN]);
            Assert.Equal("58,3 °C", t[R.BV_VORH_SA_VORLAUF_MAX]);
            Assert.Equal("12 h/a", t[R.BV_VORH_SA_VORLAUFGRENZE]);
            Assert.Equal("0 h/a", t[R.BV_VORH_SA_WIRKUNGSLOS]);
            Assert.Equal("1.234 h/a", t[R.BV_VORH_SA_SPERRZEIT]);
            // Die Zeilen folgen der Mehrwärme, in der Reihenfolge der Ergebnisspalten.
            List<string> b = Aufheizbericht.Zeilen(Gebaeude(1, Vorheizen(true)), null, DE).Select(z => z.Beschriftung).ToList();
            Assert.Equal(b.IndexOf(R.BV_VORH_MEHRWAERME) + 1, b.IndexOf(R.BV_VORH_SA_UEBERHOEHUNG));
            Assert.Equal(b.IndexOf(R.BV_VORH_SA_UEBERHOEHUNG) + 5, b.IndexOf(R.BV_VORH_SA_SPERRZEIT));
        }

        [Fact]
        public void Ohne_Wirkung_bleibt_die_Tafel_wie_beim_Vorheizen_ohne_Schalter()
        {
            string[] ohne = Tafel(Gebaeude(1, Vorheizen(false)));
            string[] mit = Tafel(Gebaeude(1, Vorheizen(true)));
            Assert.DoesNotContain(ohne, z => z.StartsWith(R.BV_VORH_SA_UEBERHOEHUNG + "=", StringComparison.Ordinal));
            Assert.Equal(ohne, mit.Take(ohne.Length));
            Assert.Equal(6, mit.Length - ohne.Length);
            Assert.Empty(Aufheizbericht.Schnellaufzeilen(Vorheizen(false), DE));
            Assert.Empty(Aufheizbericht.Schnellaufzeilen(null, DE));
            // Sollwertrampe: keine Vorheiz- und keine Schnellaufzeile.
            Assert.DoesNotContain(Tafel(Gebaeude(1, null)), z => z.StartsWith(R.BV_VORH_VERFAHREN + "=", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Zonentafel_bekommt_zwei_Spalten_nur_mit_Wirkung()
        {
            ErgebnisZoneModel Zone(int rang, string name, Vorheizkennzahlen v) => new ErgebnisZoneModel { Rang = rang, Bezeichner = name, Vorheizen = v };

            Berichtstabelle ohne = Aufheizbericht.Zonentafel(Gebaeude(1, Vorheizen(false), Zone(1, "Büro", Vorheizen(false))), DE);
            Assert.Equal(6, ohne.Kopf.Zellen.Count);
            Assert.Equal(6, ohne.Spalten.Count);
            Assert.Equal(new[] { "Büro", "6", "12,3", "3", "0,4", "1,23" }, ohne.Zeilen[0].Zellen.Select(c => c.Text).ToArray());

            Berichtstabelle mit = Aufheizbericht.Zonentafel(Gebaeude(1, Vorheizen(true),
                Zone(1, "Büro", Vorheizen(true, 1.5, 200)), Zone(2, "Lager", Vorheizen(false))), DE);
            Assert.Equal(8, mit.Kopf.Zellen.Count);
            Assert.Equal(8, mit.Spalten.Count);
            Assert.Equal(R.BV_VORH_KOPF_UEBERHOEHUNG, mit.Kopf.Zellen[6].Text);
            Assert.Equal(R.BV_VORH_KOPF_UEBERHOEHUNG_H, mit.Kopf.Zellen[7].Text);
            Assert.Equal(new[] { "Büro", "6", "12,3", "3", "0,4", "1,23", "1,5", "200" }, mit.Zeilen[0].Zellen.Select(c => c.Text).ToArray());
            Assert.Equal(Tabellenzelle.STRICH, mit.Zeilen[1].Zellen[6].Text);
            Assert.Equal(Tabellenzelle.STRICH, mit.Zeilen[1].Zellen[7].Text);
        }

        // =================================================================
        // Vorlagenfelder (Katalog v21) und Halbsatz des Kurzberichts
        // =================================================================

        [Fact]
        public void Die_Vorlagenfelder_stehen_ab_Fassung_21_mit_Format_und_Einheit()
        {
            Assert.Equal(21, Vorlagenfeldkatalog.FASSUNG_SCHNELLAUFHEIZUNG);
            Assert.True(Vorlagenfeldkatalog.KATALOGFASSUNG >= 21);
            (string Schluessel, string Format, string Einheit)[] erwartet =
            {
                ("gebaeude.ergebnis.vorheiz_ueberhoehung", "N1", "K"),
                ("gebaeude.ergebnis.vorheiz_ueberhoehung_stunden", "N0", "h/a"),
                ("gebaeude.ergebnis.vorheiz_ueberhoehung_vorlauf_max", "N1", "°C"),
                ("gebaeude.ergebnis.vorheiz_vorlaufgrenze_stunden", "N0", "h/a"),
                ("gebaeude.ergebnis.vorheiz_ueberhoehung_wirkungslos", "N0", "h/a"),
                ("gebaeude.ergebnis.vorheiz_ueberhoehung_sperrzeit", "N0", "h/a"),
            };
            foreach ((string s, string f, string e) in erwartet)
            {
                Vorlagenfeld feld = Vorlagenfeldkatalog.Finde(s);
                Assert.True(feld != null, s);
                Assert.Equal(21, feld.Seit);
                Assert.Equal(f, feld.Format);
                Assert.Equal(e, feld.Einheit);
            }
            Assert.Equal(21, Vorlagenfeldkatalog.Finde("gebaeude.ergebnis.vorheiz_schnellaufheizung").Seit);
            Assert.Equal("", Vorlagenfeldkatalog.Finde("gebaeude.ergebnis.vorheiz_schnellaufheizung").Leerwert);
            Assert.Equal(21, Vorlagenfeldkatalog.Finde("hat.schnellaufheizung").Seit);
            Assert.Equal(7, Vorlagenfeldkatalog.Alle.Count(f => f.Seit == 21 && f.Schluessel.StartsWith("gebaeude.ergebnis.", StringComparison.Ordinal)));
        }

        [Fact]
        public void Der_Halbsatz_nennt_die_Ueberhoehung_und_entfaellt_ohne_Wirkung()
        {
            Assert.Equal(", mit Schnellaufheizung (+2,0 K)", Aufheizbericht.Schnellaufhalbsatz(Vorheizen(true), DE));
            Assert.Equal(", with rapid heat-up (+1.5 K)",
                         Aufheizbericht.Schnellaufhalbsatz(Vorheizen(true, 1.5), CultureInfo.GetCultureInfo("en-US")));
            Assert.Null(Aufheizbericht.Schnellaufhalbsatz(Vorheizen(false), DE));
            Assert.Null(Aufheizbericht.Schnellaufhalbsatz(null, DE));
        }

        [Fact]
        public void Der_Datenschalter_folgt_der_Wirkung()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            daten.Varianten[0].Ergebnis.Gebaeude.Add(Gebaeude(1, Vorheizen(true)));
            daten.Varianten[1].Ergebnis.Gebaeude.Add(Gebaeude(1, Vorheizen(false)));
            Assert.True(Aufheizbericht.HatSchnellaufheizung(daten.Varianten[0]));
            Assert.False(Aufheizbericht.HatSchnellaufheizung(daten.Varianten[1]));
            Assert.True(Aufheizbericht.HatVorheizen(daten.Varianten[1]));
        }

        // =================================================================
        // Kennzahl, Export, Variantenvergleich
        // =================================================================

        [Fact]
        public void Die_Kennzahl_der_Stunden_mit_Ueberhoehung_ist_der_Hoechstwert_und_entfaellt_ohne_Wirkung()
        {
            BerichtsDaten daten = Berichtsdatenproben.Gruppendaten(2);
            daten.Varianten[0].Ergebnis.Gebaeude.AddRange(new[]
            {
                Gebaeude(1, Vorheizen(true, 2.0, 120)), Gebaeude(2, Vorheizen(true, 3.0, 340)), Gebaeude(3, Vorheizen(false)),
            });
            daten.Varianten[1].Ergebnis.Gebaeude.Add(Gebaeude(1, Vorheizen(false)));
            foreach (VariantenDaten v in daten.Varianten) KennzahlenKatalog.Berechne(v);
            Assert.Equal(340.0, daten.Varianten[0].Kennzahlen[KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZ_UEBERHOEHUNG_STUNDEN]);
            Assert.False(daten.Varianten[1].Kennzahlen.TryGetValue(KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZ_UEBERHOEHUNG_STUNDEN,
                                                                    out double? w) && w.HasValue);
            Kennzahl k = KennzahlenKatalog.Alle().Single(x => x.Schluessel == KennzahlenKatalog.SCHLUESSEL_GEB_VORHEIZ_UEBERHOEHUNG_STUNDEN);
            Assert.Equal(21, k.Seit);
            Assert.Equal("h/a", k.Einheit);
        }

        [Fact]
        public void Die_Exportschluessel_entstehen_nur_mit_Wirkung()
        {
            List<(string Schluessel, double? Wert)> s = GebaeudeErgebnisexport.Schnellaufschluessel(Vorheizen(true)).ToList();
            Assert.Equal(new[]
            {
                "VorheizUeberhoehungK", "VorheizUeberhoehungH", "VorheizUeberhoehungVorlaufMaxC", "VorheizVorlaufgrenzeH",
                "VorheizUeberhoehungWirkungslosH", "VorheizUeberhoehungSperrzeitH",
            }, s.Select(x => x.Schluessel).ToArray());
            Assert.Equal(2.0, s[0].Wert);
            Assert.Equal(58.26, s[2].Wert);
            Assert.Empty(GebaeudeErgebnisexport.Schnellaufschluessel(Vorheizen(false)));
            Assert.Empty(GebaeudeErgebnisexport.Schnellaufschluessel(null));
            // Die Schlüssel der Fassung 2 bleiben unverändert siebzehn, ohne Überschneidung.
            List<string> vorheiz = GebaeudeErgebnisexport.Vorheizschluessel(Vorheizen(true)).Select(x => x.Schluessel).ToList();
            Assert.Equal(17, vorheiz.Count);
            Assert.Empty(vorheiz.Intersect(s.Select(x => x.Schluessel)));
        }

        private static ProjektDetails Einstellung(long? schalter, double? ueberhoehung)
        {
            var ein = new DataTable();
            ein.Columns.Add("ID", typeof(long));
            ein.Columns.Add("ID_Projekt", typeof(long));
            ein.Columns.Add(SchnellaufheizungSchema.SPALTE_SCHALTER, typeof(long));
            ein.Columns.Add(SchnellaufheizungSchema.SPALTE_UEBERHOEHUNG, typeof(double));
            ein.Rows.Add(1L, 1L, (object)schalter ?? DBNull.Value, (object)ueberhoehung ?? DBNull.Value);
            return new ProjektDetails { IdProjekt = 1, Einstellungen = ein };
        }

        [Fact]
        public void Der_Variantenvergleich_meldet_Schalter_und_Ueberhoehung_NULL_ist_die_Vorgabe()
        {
            // NULL heißt „aus“ bzw. die Kern-Vorgabe 2 K — keine Abweichung.
            Assert.Empty(AbweichungsErmittler.Vergleiche(Einstellung(null, null), Einstellung(0, Vorheizvorgabe.UEBERHOEHUNG_VORGABE_K)));

            List<Abweichung> liste = AbweichungsErmittler.Vergleiche(Einstellung(null, null), Einstellung(1, 3.5));
            Abweichung Finde(string merkmal) => liste.Single(a => a.Gewerk == "Gebäude" && a.Merkmal == merkmal);
            Assert.Equal(R.ALLG_BTN_JA, Finde(R.ABW_MERKMAL_VORH_SCHNELLAUF).WertVariante);
            Assert.Equal("2,0 K", Finde(R.ABW_MERKMAL_VORH_UEBERHOEHUNG).WertStamm);
            Assert.Equal("3,5 K", Finde(R.ABW_MERKMAL_VORH_UEBERHOEHUNG).WertVariante);
            Assert.Equal(2, liste.Count);
        }
    }
}
