using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BK-5/BK-6 ohne Datenbank</b>: die Grenzwerte eines inhomogenen Aufbaus nach DIN EN ISO 6946, 6.7, gegen eine
    /// Handrechnung (synthetische Holzständerwand, runde Werte), die Gegenprobe ohne Abschnitte, die Prüfung der
    /// Flächenanteile und U_w eines Fensteraufbaus nach DIN EN ISO 10077-1.
    /// </summary>
    public class BauteilaufbauAbschnittTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static BauteilschichtModel S(double dickeM, double lambda, int? abschnitt = null)
            => new BauteilschichtModel { Dicke = dickeM, Lambda = lambda, Rho = 500, Cp = 1000, Abschnitt = abschnitt };

        /// <summary>
        /// Putz 15 mm (λ 0,7), dann EINE inhomogene Ebene 160 mm: Abschnitt 1 Holz (λ 0,13, f 0,1) neben Abschnitt 2 Dämmung
        /// (λ 0,04, f 0,9), außen Platte 20 mm (λ 0,2).
        /// </summary>
        internal static BauteilaufbauModel Staenderwand(double f1 = 0.1, double f2 = 0.9) => new BauteilaufbauModel
        {
            Bezeichner = "Probewand",
            Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
            Schichten = new List<BauteilschichtModel> { S(0.015, 0.7), S(0.16, 0.13, 1), S(0.16, 0.04, 2), S(0.02, 0.2) },
            Abschnitte = new List<BauteilabschnittModel>
            {
                new BauteilabschnittModel { Nummer = 1, Flaechenanteil = f1 },
                new BauteilabschnittModel { Nummer = 2, Flaechenanteil = f2 }
            }
        };

        [Fact]
        public void Die_Grenzwerte_folgen_der_Handrechnung()
        {
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(Staenderwand());
            Assert.True(k.Gerechnet, k.Grund);

            // Handrechnung: R_Putz 0,0214, R_Holz 1,2308, R_Daemm 4,0, R_Platte 0,1; R_si 0,13, R_se 0,04.
            double rPutz = 0.015 / 0.7, rHolz = 0.16 / 0.13, rDaemm = 0.16 / 0.04, rPlatte = 0.02 / 0.2;
            double rt1 = 0.13 + rPutz + rHolz + rPlatte + 0.04;            // Streifen Holz   1,5222
            double rt2 = 0.13 + rPutz + rDaemm + rPlatte + 0.04;           // Streifen Daemm  4,2914
            double oben = 1.0 / (0.1 / rt1 + 0.9 / rt2);                   // R'_T  = 3,6309
            double rj = 1.0 / (0.1 / rHolz + 0.9 / rDaemm);                // Ebene 3,2653
            double unten = 0.13 + rPutz + rj + rPlatte + 0.04;             // R''_T = 3,5567
            Assert.Equal(3.6309, oben, 4);
            Assert.Equal(3.5567, unten, 4);

            Assert.Equal(oben, k.RT_Oben_M2KW!.Value, 9);
            Assert.Equal(unten, k.RT_Unten_M2KW!.Value, 9);
            Assert.Equal((oben + unten) / 2.0, k.R_M2KW!.Value + 0.13 + 0.04, 9);   // R_T = 3,5938
            Assert.Equal(2.0 / (oben + unten), k.U_WM2K!.Value, 9);
            Assert.True(k.RT_Oben_M2KW >= k.RT_Unten_M2KW);
        }

        [Fact]
        public void Ohne_Abschnitte_rechnet_der_Aufbau_wie_zuvor()
        {
            // Gegenprobe: dieselben Schichten homogen - der Weg ohne Abschnitte ist bitgleich die Reihe der Widerstaende.
            BauteilaufbauModel homogen = Staenderwand();
            foreach (BauteilschichtModel s in homogen.Schichten) s.Abschnitt = null;
            homogen.Abschnitte = new List<BauteilabschnittModel>();
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(homogen);

            Assert.False(InhomogeneSchichten.Inhomogen(homogen));
            Assert.Null(k.RT_Oben_M2KW);
            Assert.Null(k.RT_Unten_M2KW);
            var schichten = homogen.Schichten.Select(s => new Schicht(s.Dicke, s.Lambda!.Value, s.Rho!.Value, s.Cp!.Value)).ToList();
            Schichtkennwerte erwartet = Bauteilreduktion.Kennwerte(schichten, Waermestromrichtung.Horizontal, 0.13, 0.04, "Probewand");
            Assert.Equal(erwartet.U_WM2K, k.U_WM2K!.Value);
            Assert.Equal(erwartet.R_M2KW, k.R_M2KW!.Value);
        }

        [Fact]
        public void Eine_falsche_Anteilsumme_rechnet_nicht_und_nennt_den_Grund()
        {
            BauteilaufbauModel m = Staenderwand(0.1, 0.8);
            string befund = InhomogeneSchichten.Pruefen(m);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BTA_ABS_MSG_SUMME, "Probewand", 0.9), befund);
            BauteilaufbauKennwerte k = BauteilaufbauCtrl.Kennwerte(m);
            Assert.False(k.Gerechnet);
            Assert.Equal(befund, k.Grund);

            // 0,995 liegt in der Toleranz 1 ± 0,01.
            Assert.Null(InhomogeneSchichten.Pruefen(Staenderwand(0.1, 0.895)));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BTA_ABS_MSG_ANTEIL, "Probewand", 1),
                         InhomogeneSchichten.Pruefen(Staenderwand(0.0, 1.0)));
        }

        [Fact]
        public void Die_Eingabepruefung_lehnt_eine_falsche_Anteilsumme_ab_und_laesst_den_homogenen_Aufbau()
        {
            Assert.Null(BauteilaufbauCtrl.EingabePruefen(Staenderwand()));
            string befund = BauteilaufbauCtrl.EingabePruefen(Staenderwand(0.1, 0.8));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, R.BTA_ABS_MSG_SUMME, "Probewand", 0.9), befund);
            // Gegenprobe: ohne Abschnitte gilt die Pruefung wie zuvor.
            BauteilaufbauModel homogen = Staenderwand();
            homogen.Schichten.RemoveAt(2);
            homogen.Schichten[1].Abschnitt = null;
            homogen.Abschnitte.Clear();
            Assert.Null(BauteilaufbauCtrl.EingabePruefen(homogen));
        }

        [Fact]
        public void Uw_folgt_der_Formel_mit_genaeherter_Glasrandlaenge()
        {
            var f = new Fensteraufbau(Ug: 1.0, Uf: 1.4, GWert: 0.5, Rahmenanteil: 0.3, PsiGlasrand: 0.04);
            // A = 2 m², A_g = 1,4 m², l_g = 4·√1,4 = 4,7329 m: U_w = 0,7·1,0 + 0,3·1,4 + 4,7329·0,04 / 2 = 1,2147.
            Assert.Equal(4.0 * Math.Sqrt(1.4), Fensterkennwerte.Glasrandlaenge(2.0, 0.3), 12);
            Assert.Equal(1.2147, Fensterkennwerte.Uw(f, 2.0)!.Value, 4);
            Assert.Equal(1.12, Fensterkennwerte.Uw(f, null)!.Value, 12);                  // ohne Flaeche kein Randterm
            Assert.Equal(1.12, Fensterkennwerte.Uw(f with { PsiGlasrand = null }, 2.0)!.Value, 12);
            Assert.Null(Fensterkennwerte.Uw(f with { Ug = null }, 2.0));
            Assert.Null(Fensterkennwerte.Uw(f with { Uf = null }, 2.0));                // Rahmen ohne U_f
            Assert.Equal(1.0, Fensterkennwerte.Uw(new Fensteraufbau(1.0, null, null, null, null), 2.0)!.Value, 12);
        }

        [Fact]
        public void Die_Fensterwerte_kommen_aus_den_Katalogfeldern_des_Aufbaus()
        {
            var m = new BauteilaufbauModel { Bauteilart = DbWerte.BAUTEILART_FENSTER };
            m.Zusatz[Fensterkennwerte.SPALTE_UG] = 1.1;
            m.Zusatz[Fensterkennwerte.SPALTE_UF] = 1.3;
            m.Zusatz[Fensterkennwerte.SPALTE_GWERT] = 0.6;
            m.Zusatz[Fensterkennwerte.SPALTE_RAHMENANTEIL] = 0.25;
            m.Zusatz[BauteilkatalogAbschnittSchema.SPALTE_PSI_GLASRAND] = 0.05;
            Fensteraufbau f = Fensterkennwerte.AusAufbau(m);
            Assert.Equal(new Fensteraufbau(1.1, 1.3, 0.6, 0.25, 0.05), f);
            Assert.True(f.Vorhanden);
            Assert.False(Fensterkennwerte.AusAufbau(new BauteilaufbauModel()).Vorhanden);
            Assert.True(Fensterkennwerte.IstFensterart(DbWerte.BAUTEILART_VORHANGFASSADE));
            Assert.False(Fensterkennwerte.IstFensterart(DbWerte.BAUTEILART_AUSSENWAND));
        }
    }

    /// <summary>BK-5/BK-6: der Schemaschritt auf der Kopie der Testdatenbank.</summary>
    [Collection("Testdatenbank")]
    public class BauteilkatalogAbschnittSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_haengt_vorlaeufig_an_227_und_ist_das_Ziel()
        {
            // Angemeldet als 229; bis 228 GeraetekostenEntfallSchema auf dem Arbeitszweig steht, haengt die Klasse an 227 + 1.
            Assert.Equal(BrennstoffpreiseTraegerkarteSchema.SCHRITT + 1, BauteilkatalogAbschnittSchema.SCHRITT);
            Assert.Equal(BauteilkatalogAbschnittSchema.SCHRITT, SchemaStand.Zielversion);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == BauteilkatalogAbschnittSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Contains("IS NULL OR", BauteilkatalogAbschnittSchema.AUFBAU_SPALTEN.Single().Typ, StringComparison.Ordinal);
        }

        [Fact]
        public void Der_Schritt_legt_Psi_Glasrand_leer_an_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BauteilkatalogAbschnittSchema.Vollstaendig());
            foreach (string t in BauteilkatalogAbschnittSchema.TABELLEN)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + t + "\" DROP COLUMN \"Psi_Glasrand\"");
            Assert.Equal(2, BauteilkatalogAbschnittSchema.Offen());
            var bericht = new List<string>();
            Assert.Equal(2, BauteilkatalogAbschnittSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("Tab_Bauteilaufbau_STAMM.Psi_Glasrand angelegt", StringComparison.Ordinal));
            Assert.True(BauteilkatalogAbschnittSchema.Vollstaendig());
            foreach (string t in BauteilkatalogAbschnittSchema.TABELLEN)
                Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"" + t + "\" WHERE \"Psi_Glasrand\" IS NOT NULL"), CultureInfo.InvariantCulture));
            var zweiter = new List<string>();
            Assert.Equal(0, BauteilkatalogAbschnittSchema.Ausfuehren(zweiter));
            Assert.Contains(zweiter, z => z.StartsWith("steht bereits", StringComparison.Ordinal));
        }

        [Fact]
        public void Die_Projektkopie_nimmt_die_Abschnitte_mit_und_rechnet_dasselbe()
        {
            if (!_db.Vorhanden) return;
            var aufbauten = new BauteilaufbauCtrl();
            BauteilaufbauCtrl.Ergebnis e = aufbauten.KatalogSpeichern(BauteilaufbauAbschnittTests.Staenderwand());
            Assert.True(e.Ok, e.Meldung);
            BauteilaufbauModel stamm = aufbauten.LesenKatalogsatz(e.Id);
            Assert.Equal(new int?[] { null, 1, 2, null }, stamm.Schichten.Select(x => x.Abschnitt).ToArray());

            long projekt = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT MIN(ID) FROM Tab_Projekt"), CultureInfo.InvariantCulture);
            int kopie = aufbauten.CopyFromStamm(e.Id, (int)projekt);
            Assert.True(kopie > 0);
            BauteilaufbauModel k = aufbauten.LesenProjektsatz(kopie);
            Assert.Equal(new int?[] { null, 1, 2, null }, k.Schichten.Select(x => x.Abschnitt).ToArray());
            Assert.Equal(new double?[] { 0.1, 0.9 }, k.Abschnitte.Select(x => x.Flaechenanteil).ToArray());
            Assert.Equal(2L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Bauteilabschnitt WHERE ID_Aufbau = ?", new DbParam("@a", kopie)), CultureInfo.InvariantCulture));
            // Die Schichten der Kopie zeigen auf die Abschnitte der Kopie, nicht des Katalogs.
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Bauteilschicht s LEFT JOIN Tab_Bauteilabschnitt a ON a.ID = s.ID_Abschnitt " +
                "WHERE s.ID_Aufbau = ? AND s.ID_Abschnitt IS NOT NULL AND (a.ID_Aufbau IS NULL OR a.ID_Aufbau <> s.ID_Aufbau)",
                new DbParam("@a", kopie)), CultureInfo.InvariantCulture));
            BauteilaufbauKennwerte ks = BauteilaufbauCtrl.Kennwerte(stamm), kk = BauteilaufbauCtrl.Kennwerte(k);
            Assert.NotNull(ks.RT_Oben_M2KW);
            Assert.Equal(ks.U_WM2K, kk.U_WM2K);
            Assert.Equal((ks.RT_Oben_M2KW, ks.RT_Unten_M2KW), (kk.RT_Oben_M2KW, kk.RT_Unten_M2KW));
        }

        [Fact]
        public void Kein_Referenzprojekt_fuehrt_Abschnitte_oder_Fensterwerte()
        {
            if (!_db.Vorhanden) return;
            long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilabschnitt\""));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau\" WHERE \"Ug\" IS NOT NULL OR \"Uf\" IS NOT NULL " +
                                  "OR \"g_Wert\" IS NOT NULL OR \"Rahmenanteil\" IS NOT NULL OR \"Psi_Glasrand\" IS NOT NULL"));
        }
    }
}
