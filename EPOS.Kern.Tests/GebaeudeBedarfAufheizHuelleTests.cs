using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Hülle der Gruppe „Aufheizung"</b> (Entwurf KP3, Welle O2; Festlegungen 22, 25, 39, 41):
    /// <see cref="GebaeudeBedarfHuelle.Aufheizung"/> liest die Ergebniszeile des Laufs, nimmt bei ausgeschalteter
    /// Optimierung die Auskunft „auch ohne Schalter" und bei Art „manuell" die bemessene Zeit der Auskunft; die Hinweise
    /// W1–W5 stehen benannt, die Zonen je Zeile. Ohne Datenbank, runde Phantasiewerte.
    /// </summary>
    public sealed class GebaeudeBedarfAufheizHuelleTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static GebaeudeBedarfErgebnis Ergebnis(ErgebnisGebaeudeModel zeile, string modell = DbWerte.GEBAEUDE_MODELL_VDI6007)
            => new GebaeudeBedarfErgebnis { Erfolgreich = true, Modell = modell, MaxLastKw = 20.0, SpitzeTagesmittelKw = 8.0, Ergebniszeile = zeile };

        private static ErgebnisGebaeudeModel Zeile(string zustand) => new ErgebnisGebaeudeModel
        {
            SpitzeKw = 20.0, SpitzeTagesmittelKw = 8.0, AufheizZustand = zustand,
            AufheizBemessung = DbWerte.AUFHEIZ_BEMESSUNG_STUNDE, AufheizArt = DbWerte.AUFHEIZ_ART_TAEGLICH,
            AufheizzeitMaxH = 6, AufheizAussenC = -12.0, AufheizLeistungKw = 12.0,
            AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_ZIEL, Aufheiztage = 150, AufheizstundenH = 600,
            AufheizzeitLaengsteH = 6, AufheiztageBegrenzt = 10, AufheiztageUnerreichbar = 0, AufheiztageNachweisband = 3,
            AufheizspruengeAus = 2, HeizleistungMaxStundenH = 0.0, AuslegungsheizlastKw = 10.0, AufheizzuschlagKw = 2.0,
        };

        [Fact]
        public void Auf_dem_Tagesbilanz_Weg_gibt_es_keine_Gruppe()
        {
            Assert.Null(GebaeudeBedarfHuelle.Aufheizung(Ergebnis(Zeile(null), DbWerte.GEBAEUDE_MODELL_TAGESBILANZ), _ => throw new InvalidOperationException()));
        }

        [Fact]
        public void Bemessen_liest_die_Ergebniszeile_und_nennt_W2_W3_W4()
        {
            GebaeudeBedarfAufheizDaten d = GebaeudeBedarfHuelle.Aufheizung(Ergebnis(Zeile(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN)),
                                                                           _ => throw new InvalidOperationException("keine Auskunft nötig"));
            Assert.Equal("BEMESSEN", d.Zustand);
            Assert.Equal("bemessen", d.Zustandtext);
            Assert.Equal(6, d.AufheizzeitMaxH);
            Assert.Equal("kälteste Stunde", d.Variante);
            Assert.Equal("täglich", d.Art);
            Assert.Equal("Zielleistung", d.Quelle);
            Assert.Equal(12.0, d.AuslegungsgroesseKw);
            Assert.Equal(20.0, d.SpitzeKw);
            Assert.Equal(8.0, d.SpitzeTagesmittelKw);
            Assert.Null(d.AufheizzeitManuellH);
            Assert.Equal(3, d.Hinweise.Count);
            Assert.StartsWith("W2", d.Hinweise[0]);
            Assert.StartsWith("W3", d.Hinweise[1]);
            Assert.StartsWith("W4", d.Hinweise[2]);
            Assert.Contains("10 Tagen", d.Hinweise[0]);
        }

        [Fact]
        public void Schalter_aus_nimmt_die_Auskunft_auch_ohne_Schalter()
        {
            bool? gefragt = null;
            GebaeudeBedarfAufheizDaten d = GebaeudeBedarfHuelle.Aufheizung(Ergebnis(new ErgebnisGebaeudeModel { SpitzeKw = 20.0 }), ohne =>
            {
                gefragt = ohne;
                return new Aufheizauskunft
                {
                    Zustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AuslegungsheizlastKw = 10.0, AufheizzuschlagKw = 2.0,
                    LeistungKw = 12.0, Skalierungsfaktor = 1.0, Quelle = DbWerte.AUFHEIZ_QUELLE_GRENZE,
                };
            });
            Assert.True(gefragt);
            Assert.True(d.SchalterAus);
            Assert.Equal("Aufheizoptimierung aus", d.Zustandtext);
            Assert.Equal(12.0, d.AuslegungsgroesseKw);
            Assert.Equal("Heizleistungsgrenze", d.Quelle);
            Assert.Empty(d.Hinweise);
            Assert.False(d.FaktorErstImLauf);
            Assert.Null(GebaeudeBedarfHuelle.Aufheizung(Ergebnis(new ErgebnisGebaeudeModel()), _ => null));
        }

        [Fact]
        public void Manuell_nennt_die_manuelle_und_die_bemessene_Zeit()
        {
            ErgebnisGebaeudeModel z = Zeile(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN);
            z.AufheizArt = DbWerte.AUFHEIZ_ART_MANUELL;
            z.AufheizzeitMaxH = 9;
            bool? gefragt = null;
            GebaeudeBedarfAufheizDaten d = GebaeudeBedarfHuelle.Aufheizung(Ergebnis(z), ohne =>
            {
                gefragt = ohne;
                return new Aufheizauskunft { Zustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizzeitMaxH = 6 };
            });
            Assert.False(gefragt);
            Assert.Equal(9, d.AufheizzeitManuellH);
            Assert.Equal(6, d.AufheizzeitMaxH);
            Assert.Equal("manuell (9 h)", d.Art);
        }

        [Fact]
        public void Unerreichbar_und_gekoppelt_tragen_W1_und_W5()
        {
            ErgebnisGebaeudeModel u = Zeile(DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR);
            u.AufheizzeitMaxH = null;
            u.AufheiztageUnerreichbar = 30;
            u.AufheiztageBegrenzt = 0; u.AufheiztageNachweisband = 0; u.AufheizspruengeAus = 0;
            GebaeudeBedarfAufheizDaten d = GebaeudeBedarfHuelle.Aufheizung(Ergebnis(u), _ => null);
            Assert.Equal(2, d.Hinweise.Count);
            Assert.Contains("unerreichbar", d.Hinweise[0]);
            Assert.Contains("30 Tagen", d.Hinweise[1]);

            var g = new ErgebnisGebaeudeModel { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_GEKOPPELT, HeizleistungMaxStundenH = 4.5 };
            GebaeudeBedarfAufheizDaten k = GebaeudeBedarfHuelle.Aufheizung(Ergebnis(g), _ => null);
            Assert.Equal("gekoppelt — nicht optimiert", k.Zustandtext);
            Assert.Equal("", k.Art);
            Assert.Null(k.AuslegungsgroesseKw);
            Assert.StartsWith("W5", Assert.Single(k.Hinweise));
        }

        [Fact]
        public void Zonen_stehen_ab_zwei_Zonen_je_Zeile()
        {
            GebaeudeBedarfErgebnis e = Ergebnis(Zeile(DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN));
            e.Zonen.Add(new GebaeudeBedarfZone
            {
                Name = "Zone 1",
                Ergebniszeile = new ErgebnisZoneModel { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_BEMESSEN, AufheizzeitMaxH = 4, AufheizLeistungKw = 5.0, AufheizLeistungsquelle = DbWerte.AUFHEIZ_QUELLE_ZIEL },
            });
            e.Zonen.Add(new GebaeudeBedarfZone
            {
                Name = "Zone 2",
                Ergebniszeile = new ErgebnisZoneModel { AufheizZustand = DbWerte.AUFHEIZ_ZUSTAND_UNBEHEIZT },
            });
            GebaeudeBedarfAufheizDaten d = GebaeudeBedarfHuelle.Aufheizung(e, _ => null);
            Assert.Equal(2, d.Zonen.Count);
            Assert.Equal("Zielleistung", d.Zonen[0].Quelle);
            Assert.Equal("unbeheizt — ohne Rampe", d.Zonen[1].Zustandtext);
        }
    }
}
