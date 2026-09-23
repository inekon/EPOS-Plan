using System;
using System.Collections.Generic;
using System.IO;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c2 (E7c1‑Q7, Mockup U22 „Wirkung Jahr 1") — der Jahresbetrag des
    /// KWK-Zuschlags, ausgelagert aus den zwei Reihen des Laufs (je Anlage und Ersatzweg),
    /// damit die Überlagerung „Sätze und Herkunft" ihn RUFT statt ihn abzuschreiben. Die
    /// Ausdrücke sind Zeichen für Zeichen die der Schleifen; der Basislauf blieb bitgleich
    /// (A/B 13 Projekte).
    /// </summary>
    public class KwkgJahresbetragTests
    {
        [Fact]
        public void Voll_ist_Menge_mal_Satz_je_Stromart()
        {
            // 200 MWh × 3,5 ct/kWh + 75 MWh × 7 ct/kWh = 7.000 + 5.250 €.
            Assert.Equal(12250.0, KwkgJahresbetrag.Voll(200, 3.5, 75, 7.0), 9);
            Assert.Equal(7000.0, KwkgJahresbetrag.Voll(200, 3.5, 0, 0), 9);
            Assert.Equal(0.0, KwkgJahresbetrag.Voll(0, 0, 0, 0));
        }

        [Fact]
        public void Verguetet_begrenzen_Deckel_Rest_und_Abschlag()
        {
            Assert.Equal(3100.0, KwkgJahresbetrag.VerguetetH(5500, 3100, 30000, 0), 9);
            Assert.Equal(2000.0, KwkgJahresbetrag.VerguetetH(5500, 3100, 2000, 0), 9);
            Assert.Equal(2500.0, KwkgJahresbetrag.VerguetetH(2500, 3100, 30000, 0), 9);
            Assert.Equal(2790.0, KwkgJahresbetrag.VerguetetH(5500, 3100, 30000, 0.1), 9);
        }

        [Fact]
        public void Abschlag_ist_ein_Anteil_zwischen_null_und_eins()
        {
            Assert.Equal(0.05, KwkgJahresbetrag.Abschlag(5), 12);
            Assert.Equal(0.0, KwkgJahresbetrag.Abschlag(-3));
            Assert.Equal(1.0, KwkgJahresbetrag.Abschlag(250));
        }

        [Fact]
        public void Staffeldeckel_nimmt_die_letzte_Zeile_bis_zum_Jahr()
        {
            IReadOnlyList<KeyValuePair<int, double>> s = KwkgJahresbetrag.STAFFEL_RUECKFALL;
            Assert.Equal(5000.0, KwkgJahresbetrag.StaffelDeckel(s, 2019));   // vor der ersten Zeile
            Assert.Equal(3300.0, KwkgJahresbetrag.StaffelDeckel(s, 2026));
            Assert.Equal(3100.0, KwkgJahresbetrag.StaffelDeckel(s, 2027));
            Assert.Equal(2500.0, KwkgJahresbetrag.StaffelDeckel(s, 2040));
            Assert.Equal(3500.0, KwkgJahresbetrag.StaffelDeckel(new List<KeyValuePair<int, double>>(), 2030));
        }

        [Fact]
        public void Staffeldeckel_ueber_den_Katalog_faellt_auf_die_Rueckfallstaffel()
        {
            Func<string, int, GesetzParameter> katalog = (s, j) =>
                s == DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL
                    ? new GesetzParameter(1, s, "KWKG", 2027, 2950, "h/a", "", "") : null!;
            Assert.Equal(2950.0, KwkgJahresbetrag.StaffelDeckel(katalog, 2027));
            Assert.Equal(3300.0, KwkgJahresbetrag.StaffelDeckel((Func<string, int, GesetzParameter>)null!, 2026));
            Assert.Equal(3100.0, KwkgJahresbetrag.StaffelDeckel((s, j) => throw new InvalidOperationException(), 2027));
        }

        [Fact]
        public void Ohne_Tatbestand_Keiner_bleibt_der_Eigenstromsatz()
        {
            Assert.Equal(3.5, KwkgJahresbetrag.SatzEigenWirksam(3.5, DbWerte.KWKG_EIGENFALL_NR2));
            Assert.Equal(3.5, KwkgJahresbetrag.SatzEigenWirksam(3.5, ""));
            Assert.Equal(0.0, KwkgJahresbetrag.SatzEigenWirksam(3.5, DbWerte.KWKG_EIGENFALL_KEINER));
            Assert.Equal(0.0, KwkgJahresbetrag.SatzEigenWirksam(3.5, " " + DbWerte.KWKG_EIGENFALL_KEINER + " "));
            Assert.Equal(0.0, KwkgJahresbetrag.SatzEigenWirksam(null, DbWerte.KWKG_EIGENFALL_NR2));
        }

        /// <summary>
        /// <b>Die Wache:</b> Der Lauf schreibt den Jahresbetrag nicht ein zweites Mal aus —
        /// beide Reihen rufen <see cref="KwkgJahresbetrag"/>, sonst könnten Lauf und
        /// „Wirkung Jahr 1" auseinanderlaufen. Gemessen wird die Quelle, wie in
        /// <c>KwkgSatzHerkunftTests</c>.
        /// </summary>
        [Fact]
        public void Der_Lauf_ruft_den_Jahresbetrag_statt_ihn_abzuschreiben()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;
            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");

            string quelle = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.Kern", "Allgemein",
                                                          "Wirtschaftlichkeit", "WirtschaftlichkeitCtrl.cs"));
            Assert.DoesNotContain("1000.0 * (satz", quelle);
            Assert.DoesNotContain("* (1.0 - abschlag)", quelle);
            Assert.Contains("KwkgJahresbetrag.Voll(", quelle);
            Assert.Contains("KwkgJahresbetrag.VerguetetH(", quelle);
        }
    }
}
