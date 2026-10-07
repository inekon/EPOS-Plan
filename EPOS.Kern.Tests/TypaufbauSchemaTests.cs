using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>BA-2 — Schemaschritt <see cref="TypaufbauSchema"/> und die Saat der Typaufbauten</b> ohne Datenbank: Kette,
    /// Paketanhebung, Prüfklausel, Saat (Codes, Schichtzahl, Normbaustoffe, Lage der Dämmung, U und C₁ über
    /// <see cref="Bauteilreduktion"/>), Nachziehstellen in Migration, Werkzeug und Testvorrichtung.
    /// </summary>
    public sealed class TypaufbauSchemaTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        [Fact]
        public void Der_Schritt_haengt_an_der_Kette_und_der_Zielstand_traegt_ihn()
        {
            Assert.Equal(192, TypaufbauSchema.SCHRITT);
            Assert.Equal(RaumgrundrissSchema.SCHRITT + 1, TypaufbauSchema.SCHRITT);
            Assert.Equal(TypaufbauSchema.SCHRITT + 1, StandardlastprofilSchema.SCHRITT);   // danach Schritt 193 (SLP25)
            Assert.True(SchemaStand.Zielversion >= TypaufbauSchema.SCHRITT);
            Paketanhebung.Stufe s = Paketanhebung.Stufen.Single(x => x.Nr == TypaufbauSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, s.Wirkung);
            Assert.Null(s.Umformung);
        }

        [Fact]
        public void Die_Spalte_ist_nullbar_und_prueft_auf_die_Codes()
        {
            string sql = TypaufbauSchema.SqlSpalte(TypaufbauSchema.TAB_PROJEKT);
            Assert.StartsWith("ALTER TABLE \"Tab_Bauteilaufbau\" ADD COLUMN \"Typaufbau\" TEXT CHECK (\"Typaufbau\" IS NULL OR \"Typaufbau\" IN (",
                              sql, StringComparison.Ordinal);
            Assert.DoesNotContain("NOT NULL", sql);
            foreach (string c in TypaufbauSaattabelle.Codes) Assert.Contains("'" + c + "'", sql);
            Assert.Contains("Typaufbau", Katalogfassung.Tabelle(TypaufbauSchema.TAB_STAMM).Fachspalten);
        }

        [Fact]
        public void Die_Saat_traegt_neun_Typen_mit_drei_bis_vier_Schichten_aus_der_Normsaat()
        {
            IReadOnlyList<TypaufbauSaat> alle = TypaufbauSaattabelle.Alle;
            Assert.Equal(9, alle.Count);
            Assert.Equal(alle.Count, alle.Select(t => t.Code).Distinct().Count());
            Assert.Equal(alle.Count, alle.Select(t => t.Bezeichner).Distinct().Count());
            foreach (TypaufbauSaat t in alle)
            {
                Assert.InRange(t.Schichten.Count, 3, 4);
                Assert.InRange(t.Abgleichschicht, 0, t.Schichten.Count - 1);
                Assert.Contains(t.Bauteilart, new[] { DbWerte.BAUTEILART_AUSSENWAND, DbWerte.BAUTEILART_DACH, DbWerte.BAUTEILART_BODENPLATTE });
                Assert.True(t.Bezeichner.Length <= BaustoffSchema.LAENGE_BEZEICHNER && t.Beschreibung.Length <= 250, t.Code);
                foreach (TypaufbauSchicht s in t.Schichten)
                {
                    BaustoffSaat b = s.Baustoff;
                    Assert.Null(b.Hersteller);                       // herstellerneutral
                    Assert.InRange(b.Id, 1, 65);
                    Assert.Contains("DIN", b.Quelle);
                    Assert.True(s.Dicke_M > 0.0 && s.Dicke_M <= 0.5, t.Code);
                }
                BaustoffSaat abgleich = t.Schichten[t.Abgleichschicht].Baustoff;
                if (t.Gedaemmt) Assert.Equal("Dämmstoffe", abgleich.Gruppe);
                else Assert.NotEqual("Dämmstoffe", abgleich.Gruppe);
            }
            Assert.True(TypaufbauSaattabelle.QUELLE.Length <= 120);
        }

        /// <summary>Die Lage der Dämmung (Konzept 4.1): außen, innen, im Gefach, über bzw. unter dem Beton.</summary>
        [Theory]
        [InlineData(TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT, 2, 1)]   // Dämmung außen vor dem Kalksandstein
        [InlineData(TypaufbauSaattabelle.AW_MASSIV_INNENGEDAEMMT, 1, 2)]    // Dämmung raumseitig vor dem Ziegel
        [InlineData(TypaufbauSaattabelle.DA_STAHLBETON_GEDAEMMT, 2, 1)]     // Dämmung über dem Beton
        [InlineData(TypaufbauSaattabelle.BO_DAEMMUNG_OBEN, 1, 2)]           // Dämmung unter dem Estrich, über der Platte
        [InlineData(TypaufbauSaattabelle.BO_DAEMMUNG_UNTEN, 2, 1)]          // Dämmung unter der Decke
        public void Die_Lage_der_Daemmung_ist_die_der_Bauart(string code, int daemmung, int tragend)
        {
            TypaufbauSaat t = TypaufbauSaattabelle.Zu(code);
            Assert.Equal(daemmung, t.Abgleichschicht);
            Assert.Equal("Dämmstoffe", t.Schichten[daemmung].Baustoff.Gruppe);
            Assert.True(t.Schichten[tragend].Baustoff.Rho >= 1800.0, code);
            Assert.NotEqual("Dämmstoffe", t.Schichten[0].Baustoff.Gruppe);      // raumseitig keine Dämmung zuerst
        }

        /// <summary>Jeder Typaufbau rechnet über die Reduktion ein plausibles U und ein positives C₁.</summary>
        [Fact]
        public void Jeder_Typaufbau_rechnet_ein_plausibles_U_und_C1()
        {
            foreach (TypaufbauSaat t in TypaufbauSaattabelle.Alle)
            {
                BauteilaufbauModel m = TypaufbauSaattabelle.AlsModell(t);
                Assert.Equal(DbWerte.HERKUNFT_VORGABE, m.Herkunft);
                Assert.Equal(t.Code, m.Typaufbau);
                double neigung = t.Bauteilart == DbWerte.BAUTEILART_DACH ? 0.0 : t.Bauteilart == DbWerte.BAUTEILART_BODENPLATTE ? 180.0 : 90.0;
                Bauteilrand rand = t.Code == TypaufbauSaattabelle.BO_DAEMMUNG_OBEN ? Bauteilrand.Erdreich : Bauteilrand.Aussenluft;
                double? u = Ersatzaufbau.U(m, neigung, rand);
                Assert.NotNull(u);
                Assert.InRange(u.Value, 0.1, 2.0);
                var schichten = m.Schichten.Select((x, i) => GebaeudeZonenabbildung.AlsSchicht(x, i + 1, m.Bezeichner)).ToList();
                Bauteilkennwerte k = Bauteilreduktion.Reduzieren(schichten, 1.0, 5.0, Bauteilreduktion.RichtungAusNeigung(neigung));
                Assert.True(k.C1_Jk > 5_000.0, t.Code + ": C1 " + k.C1_Jk.ToString(CultureInfo.InvariantCulture));
                Assert.True(k.R1_KW > 0.0, t.Code);
            }
        }

        [Fact]
        public void Migration_Werkzeug_und_Vorrichtung_fuehren_den_Schritt()
        {
            string wurzel = Repo();
            if (wurzel == null) return;
            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein", "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_TYPAUFBAU = TypaufbauSchema.SCHRITT;", migration);
            Assert.Contains("new Schritt(SCHRITT_TYPAUFBAU", migration);
            Assert.Contains("TypaufbauSchema.Ausfuehren(", File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs")));
            Assert.Contains("TypaufbauSchema.Ausfuehren(null);", File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs")));
        }

        private static string Repo()
        {
            string d = AppContext.BaseDirectory;
            while (d != null && !File.Exists(Path.Combine(d, "WP-Plan.sln"))) d = Path.GetDirectoryName(d);
            return d;
        }
    }

    /// <summary>
    /// <b>BA-2 — der Schritt an der Testdatenbank</b>: Spalten, CHECK, Saat samt Schichten und Katalogschlüssel,
    /// Wiederholbarkeit und Duplizieren eines Projekts mit Ersatzaufbau.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TypaufbauSchemaDatenbankTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        [Fact]
        public void Spalten_Saat_und_Schluessel_stehen_und_der_Schritt_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(TypaufbauSchema.SpaltenVorhanden());
            Assert.True(TypaufbauSchema.Vollstaendig());
            Assert.Empty(TypaufbauSchema.FehlendeTypen());
            Assert.Equal(9L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"ReadOnly\" = ? AND \"Typaufbau\" IS NOT NULL", new DbParam("@r", 1)));
            Assert.Equal(9L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"Typaufbau\" IS NOT NULL AND \"Herkunft\" = ? " +
                                  "AND \"Katalog_Schluessel\" LIKE ? AND length(\"Katalog_Pruefsumme\") = ?",
                                  new DbParam("@h", DbWerte.HERKUNFT_VORGABE), new DbParam("@k", "BTA:%"), new DbParam("@l", 64)));
            int schichten = TypaufbauSaattabelle.Alle.Sum(t => t.Schichten.Count);
            Assert.Equal((long)schichten, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht_STAMM\" s JOIN \"Tab_Bauteilaufbau_STAMM\" a " +
                                               "ON a.\"ID\" = s.\"ID_Aufbau\" JOIN \"Tab_Baustoff_STAMM\" b ON b.\"ID\" = s.\"ID_Baustoff\" " +
                                               "WHERE a.\"Typaufbau\" IS NOT NULL AND s.\"Lambda\" = b.\"Lambda\" AND s.\"cp\" = b.\"cp\""));
            Assert.Equal(0, TypaufbauSchema.Ausfuehren(null));
            Assert.Equal(9L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilaufbau_STAMM\" WHERE \"Typaufbau\" IS NOT NULL"));
        }

        [Fact]
        public void Der_CHECK_weist_einen_fremden_Code_ab()
        {
            if (!_db.Vorhanden) return;
            int projekt = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT MIN(\"ID\") FROM \"Tab_Projekt\""), CultureInfo.InvariantCulture);
            Assert.ThrowsAny<Exception>(() =>
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren("INSERT INTO \"Tab_Bauteilaufbau\" (\"ID_Projekt\", \"Bezeichner\", \"Typaufbau\") VALUES (?, ?, ?)",
                                 new DbParam("@p", projekt), new DbParam("@b", "fremd"), new DbParam("@t", "KEIN_TYP"));
                    v.Commit();
                }
            });
        }

        [Fact]
        public void Duplizieren_traegt_das_Kennzeichen_mit()
        {
            if (!_db.Vorhanden) return;
            const string name = "Laurentiuskirche";
            object o = DataRepository.ExecuteScalar("SELECT \"ID\" FROM \"Tab_Projekt\" WHERE \"Projektname\" = ?", new DbParam("@n", name));
            if (o == null || o == DBNull.Value) return;
            int projekt = Convert.ToInt32(o, CultureInfo.InvariantCulture);
            BauteilaufbauModel m = TypaufbauSaattabelle.AlsModell(TypaufbauSaattabelle.Zu(TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT));
            foreach (BauteilschichtModel s in m.Schichten) s.ID_Baustoff = null;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                BauteilaufbauCtrl.ProjektaufbauEinfuegen(v, projekt, m);
                v.Commit();
            }
            int neu = new ProjektDuplizierenCtrl().Duplizieren(name, name + " BA-2");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.Equal(TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT \"Typaufbau\" FROM \"Tab_Bauteilaufbau\" WHERE \"ID_Projekt\" = ? AND \"Bezeichner\" = ?",
                new DbParam("@p", neu), new DbParam("@b", m.Bezeichner)), CultureInfo.InvariantCulture));
            Assert.Equal(4L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteilschicht\" WHERE \"ID_Aufbau\" IN (SELECT \"ID\" FROM \"Tab_Bauteilaufbau\" " +
                                  "WHERE \"ID_Projekt\" = ? AND \"Typaufbau\" IS NOT NULL)", new DbParam("@p", neu)));
        }
    }
}
