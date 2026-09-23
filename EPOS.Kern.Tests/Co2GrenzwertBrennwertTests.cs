using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>ETAPPE E7 — Konzept § 6.3 Nr. 29: am CO₂-Grenzwert gilt immer der
    /// Brennwert</b> (Register R‑NR Nr. 29, Anwender 22.09.2026).
    ///
    /// <para>Der Grenzwert 270 g/kWh des § 2 StromStG wird brennwertbezogen geprüft:
    /// Der Zähler nimmt den Ho-Faktor — den brennwertbezogenen Katalogwert, wo der
    /// Katalog einen führt (Erdgas 181,4 g/kWh), sonst den heizwertbezogenen,
    /// umgerechnet über <c>H_i / H_s</c> des Trägers. Ohne gepflegten Brennwert bleibt
    /// der heizwertbezogene Faktor (konservativ) und eine Begründung sagt es.</para>
    ///
    /// <para>Rein: keine Datenbank, der Katalog ist die Vorbelegung des
    /// <see cref="GesetzKatalog"/>. Die Kultur ist gepinnt, weil Begründung und
    /// Herleitung aus den Ressourcen kommen.</para>
    /// </summary>
    public class Co2GrenzwertBrennwertTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        private const int JAHR = 2026;

        /// <summary>Erdgas: Heizwert und Brennwert je m³ (Katalogwerte des Beispielprojekts).</summary>
        private const double ERDGAS_HI = 10.5, ERDGAS_HS = 11.6;

        /// <summary>Heizöl EL: Heizwert und Brennwert je Liter (runde Prüfwerte).</summary>
        private const double OEL_HI = 10.0, OEL_HS = 10.6;

        private static Func<string, GesetzParameter> Saetze()
        {
            IList<GesetzParameter> vor = GesetzKatalog.Vorbelegung();
            return s => vor.Where(p => p.Schluessel == s && p.JahrVon <= JAHR)
                           .OrderByDescending(p => p.JahrVon)
                           .FirstOrDefault();
        }

        private static double Katalog(string schluessel) => Saetze()(schluessel).Wert.Value;

        private static SteuerAnlage Anlage(string schluesselCo2, double hi, double hs,
                                           double brennstoff, double strom, double waerme)
        {
            return new SteuerAnlage
            {
                Bezeichner = "BHKW",
                PelKW = 300.0,
                BrennstoffMWh = brennstoff,
                StromMWh = strom,
                WaermeMWh = waerme,
                SchluesselCo2 = schluesselCo2,
                EffHi = hi,
                EffHs = hs,
                Fossil = true,
                Stromerzeuger = true
            };
        }

        private static SteuerErgebnis Befreiung(SteuerAnlage a)
        {
            var e = new SteuerEingabe
            {
                Unternehmensart = DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                HocheffizienzNachweis = true,
                RaeumlicherZusammenhang = true,
                KwkEigenMWh = a.StromMWh
            };
            e.Anlagen.Add(a);
            return SteuerGutschriftRechner.Rechne(e, JAHR, Saetze(), DE);
        }

        // =====================================================================
        //  Der Leser
        // =====================================================================

        /// <summary>Zu Erdgas führt der Katalog einen brennwertbezogenen Schlüssel, zu
        /// allen anderen Trägern nicht.</summary>
        [Fact]
        public void Zum_Erdgas_gibt_es_den_brennwertbezogenen_Schluessel()
        {
            Assert.Equal(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO,
                         SteuerGutschriftRechner.Co2SchluesselBrennwert(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI));
            Assert.Null(SteuerGutschriftRechner.Co2SchluesselBrennwert(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL));
            Assert.Null(SteuerGutschriftRechner.Co2SchluesselBrennwert(DbWerte.GESETZ_EF_BILANZ_EBEV_FLUESSIGGAS));
            Assert.Null(SteuerGutschriftRechner.Co2SchluesselBrennwert(""));
        }

        /// <summary>
        /// Erdgas nimmt den Katalogwert 181,4 g/kWh (H_s) — nicht die Umrechnung über die
        /// Heizwerte (200,9 × 10,5 / 11,6 = 181,85), denn der Katalog führt den
        /// brennwertbezogenen Faktor selbst.
        /// </summary>
        [Fact]
        public void Erdgas_nimmt_den_brennwertbezogenen_Katalogfaktor()
        {
            SteuerAnlage a = Anlage(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI, ERDGAS_HI, ERDGAS_HS,
                                    4342.105, 1650.0, 1953.947);

            Co2Energieertrag c = SteuerGutschriftRechner.Co2JeEnergieertrag(a, Saetze());

            Assert.NotNull(c);
            Assert.Equal(Co2Bezug.KatalogBrennwert, c.Bezug);
            Assert.Equal(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HO, c.Schluessel);
            Assert.Equal(181.4, c.FaktorGJeKwh, 6);
            Assert.Null(c.QuotientHiHs);

            // Beispielprojekt (Rechenweg 05): 181,4 × 4.342,105 / (1.650 + 1.953,947)
            // = 218,55 g/kWh — vor E7 waren es mit dem Hi-Faktor 242,05 g/kWh.
            Assert.Equal(181.4 * 4342.105 / (1650.0 + 1953.947), c.GrammJeKwh, 9);
            Assert.Equal(218.55, Math.Round(c.GrammJeKwh, 2), 2);
        }

        /// <summary>
        /// Heizöl: Der Katalog führt nur den heizwertbezogenen Faktor (266,4 g/kWh); er
        /// wird über die gepflegten Werte des Trägers umgerechnet —
        /// 266,4 × 10,0 / 10,6 = 251,32 g/kWh (H_s).
        /// </summary>
        [Fact]
        public void Heizoel_wird_ueber_die_Heizwerte_des_Traegers_umgerechnet()
        {
            SteuerAnlage a = Anlage(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL, OEL_HI, OEL_HS,
                                    1000.0, 350.0, 550.0);

            Co2Energieertrag c = SteuerGutschriftRechner.Co2JeEnergieertrag(a, Saetze());

            Assert.NotNull(c);
            Assert.Equal(Co2Bezug.Umgerechnet, c.Bezug);
            Assert.Equal(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL, c.Schluessel);
            Assert.Equal(Katalog(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL), c.KatalogfaktorGJeKwh, 9);
            Assert.Equal(OEL_HI / OEL_HS, c.QuotientHiHs.Value, 12);
            Assert.Equal(266.4 * OEL_HI / OEL_HS, c.FaktorGJeKwh, 9);
            Assert.Equal(266.4 * OEL_HI / OEL_HS * 1000.0 / 900.0, c.GrammJeKwh, 9);
        }

        /// <summary>
        /// Ohne gepflegten Brennwert gibt es keinen Weg zum Ho-Faktor: Der
        /// heizwertbezogene Faktor bleibt — konservativ, denn der Wert fällt dadurch zu
        /// hoch aus —, und die Begründung nennt die Anlage. Sie ist ein Hinweis, kein
        /// Grund einer Nullzeile: <c>PositionsGruende</c> bleibt leer.
        /// </summary>
        [Fact]
        public void Ohne_Brennwert_bleibt_der_heizwertbezogene_Faktor_und_die_Begruendung_nennt_es()
        {
            SteuerAnlage a = Anlage(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL, OEL_HI, 0.0,
                                    1000.0, 350.0, 650.0);

            Co2Energieertrag c = SteuerGutschriftRechner.Co2JeEnergieertrag(a, Saetze());
            Assert.NotNull(c);
            Assert.Equal(Co2Bezug.Heizwert, c.Bezug);
            Assert.Equal(266.4, c.FaktorGJeKwh, 9);

            SteuerErgebnis r = Befreiung(a);                       // 266,4 × 1,0 < 270
            Assert.True(r.StromsteuerBefreiungEur > 0);
            Assert.Contains(r.Begruendungen, g => g.Contains("kein Brennwert (Ho) gepflegt", StringComparison.Ordinal)
                                                  && g.Contains("BHKW (300 kW)", StringComparison.Ordinal));
            Assert.False(r.PositionsGruende.ContainsKey(SteuerPosition.STROMST_BEFREIUNG));
        }

        // =====================================================================
        //  Die Rechenwirkung — der Vorher/Nachher-Fall für Heizöl
        // =====================================================================

        /// <summary>
        /// <b>Der Grenzfall für einen Träger ohne brennwertbezogenen Katalogwert.</b>
        /// Brennstoff 1.050 MWh, Energieertrag 1.000 MWh (400 Strom + 600 Wärme):
        /// <code>
        /// ALT (Hi-Faktor):   266,4  × 1,05 = 279,7 g/kWh  ⇒ keine Befreiung, 0,00 €/a
        /// NEU (Ho, umgerechnet): 251,32 × 1,05 = 263,9 g/kWh ⇒ 400 × 20,50 = 8.200,00 €/a
        /// </code>
        /// </summary>
        [Fact]
        public void Heizoel_im_Grenzfall_wird_mit_dem_Brennwert_befreit()
        {
            SteuerAnlage a = Anlage(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL, OEL_HI, OEL_HS,
                                    1050.0, 400.0, 600.0);

            // Der alte Zähler läge über dem Grenzwert …
            Assert.True(266.4 * 1050.0 / 1000.0 >= 270.0);

            SteuerErgebnis r = Befreiung(a);

            // … der neue liegt darunter: 8.200,00 €/a statt 0,00 €/a.
            Assert.Equal(8200.00, r.StromsteuerBefreiungEur, 2);
            Assert.Contains(r.Herkunft, h => h.Contains("263,9 g/kWh", StringComparison.Ordinal) &&
                                             h.Contains("H_i/H_s 0,9434", StringComparison.Ordinal));
        }

        /// <summary>
        /// Über dem Grenzwert nennt die Begründung den brennwertbezogenen Wert je
        /// Anlage — die Zahl, an der die Befreiung scheitert, nicht nur die Anlage.
        /// </summary>
        [Fact]
        public void Ueber_dem_Grenzwert_nennt_die_Begruendung_den_brennwertbezogenen_Wert()
        {
            // Energieertrag 80 % des Brennstoffs: 251,32 / 0,8 = 314,2 g/kWh (H_s).
            SteuerAnlage a = Anlage(DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL, OEL_HI, OEL_HS,
                                    1000.0, 300.0, 500.0);

            SteuerErgebnis r = Befreiung(a);

            Assert.Equal(0.0, r.StromsteuerBefreiungEur, 6);
            string grund = r.PositionsGruende[SteuerPosition.STROMST_BEFREIUNG];
            Assert.Contains("270", grund, StringComparison.Ordinal);
            Assert.Contains("brennwertbezogen", grund, StringComparison.Ordinal);
            Assert.Contains("314,2 g/kWh", grund, StringComparison.Ordinal);
        }

        /// <summary>
        /// Die Herleitung steht nur dort, wo gerechnet wurde: Mit Befreiung trägt die
        /// Herkunft die Zeile „brennwertbezogen geprüft" samt Grenzwert.
        /// </summary>
        [Fact]
        public void Mit_Befreiung_traegt_die_Herkunft_die_Herleitung_der_CO2_Pruefung()
        {
            SteuerAnlage a = Anlage(DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI, ERDGAS_HI, ERDGAS_HS,
                                    4342.105, 1650.0, 1953.947);

            SteuerErgebnis r = Befreiung(a);

            Assert.True(r.StromsteuerBefreiungEur > 0);
            string zeile = r.Herkunft.Single(h => h.Contains("brennwertbezogen geprüft", StringComparison.Ordinal));
            Assert.Contains("BHKW (300 kW): 218,6 g/kWh (EBeV 181,4 g/kWh, brennwertbezogen)", zeile,
                            StringComparison.Ordinal);
            Assert.Contains("Grenzwert 270 g/kWh", zeile, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ein nicht fossiler Träger kennt keinen Grenzwert — und damit auch keine
        /// Herleitungszeile der CO₂-Prüfung.
        /// </summary>
        [Fact]
        public void Nicht_fossil_ohne_Herleitung()
        {
            SteuerAnlage a = Anlage("", ERDGAS_HI, ERDGAS_HS, 1000.0, 400.0, 500.0);
            a.Fossil = false;

            SteuerErgebnis r = Befreiung(a);

            Assert.True(r.StromsteuerBefreiungEur > 0);
            Assert.DoesNotContain(r.Herkunft, h => h.Contains("brennwertbezogen geprüft", StringComparison.Ordinal));
        }
    }
}
