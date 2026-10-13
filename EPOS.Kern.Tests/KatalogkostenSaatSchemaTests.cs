using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Schemaschritt „Saat der Katalogkosten-Satzvorlagen"</b> (<see cref="KatalogkostenSaatSchema"/>, KB-3):
    /// Nummer und Register, die sechzehn Verweise an den acht Projektkopien, und die drei Zusagen der Saat —
    /// jede Zeile mit Kosten trägt einen Verweis, kein Satz mehr als einen je Art, ein zweiter Lauf ändert nichts.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogkostenSaatSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_ist_226_und_steht_im_Register()
        {
            Assert.Equal(226, KatalogkostenSaatSchema.SCHRITT);
            // Die Kette hängt an 225 ZonenLastfelderSchema (Sitzung Gebäudesimulation, DA-2).
            Assert.Equal(ZonenLastfelderSchema.SCHRITT + 1, KatalogkostenSaatSchema.SCHRITT);
            // Den Zielstand hält seit KB-2 der Schritt 227 (BrennstoffpreiseTraegerkarteSchema); 226 liegt
            // darunter oder darauf.
            Assert.True(SchemaStand.Zielversion >= KatalogkostenSaatSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + KatalogkostenSaatSchema.SCHRITT + ".");
            Assert.Contains(Paketanhebung.Stufen,
                            s => s.Nr == KatalogkostenSaatSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
        }

        [Fact]
        public void Acht_Projektkopien_tragen_die_zwei_Verweise_auf_die_Kostenvorlage()
        {
            Assert.Equal(8, KatalogkostenSaatSchema.GEWERKE.Count);
            Assert.Equal(16, KatalogkostenSaatSchema.SPALTEN.Count);
            // Dieselben acht Gewerke wie die Kataloge der Schritte 208/209 - eine Liste, nicht zwei.
            Assert.Equal(KatalogkostenUrsprungSchema.KATALOGE_MIT_KOSTEN.OrderBy(x => x, StringComparer.Ordinal),
                         KatalogkostenSaatSchema.GEWERKE.Select(g => g.Katalog).OrderBy(x => x, StringComparer.Ordinal));
            if (!_db.Vorhanden) return;
            Assert.True(KatalogkostenSaatSchema.SpaltenStehen());
            foreach ((string tabelle, string spalte, _) in KatalogkostenSaatSchema.SPALTEN)
                Assert.Contains(Verweise(tabelle),
                                f => f.Spalte == spalte && f.Ziel == KatalogkostenSaatSchema.TAB_KOSTENVORLAGE);
        }

        [Fact]
        public void Jede_Zeile_mit_Kosten_traegt_genau_einen_Verweis_je_Art()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KatalogkostenSaatSchema.Vollstaendig());
            Assert.Equal(0, KatalogkostenSaatSchema.Offen());

            int gesaet = 0;
            foreach (Katalogkostengewerk g in KatalogkostenSaatSchema.GEWERKE)
                foreach (string tabelle in new[] { g.Katalog, g.Kopie })
                    foreach (int kategorie in new[] { DbWerte.KOSTEN_KATEGORIE_INVESTITION, DbWerte.KOSTEN_KATEGORIE_BETRIEB })
                    {
                        string verweis = KatalogkostenSaatSchema.Verweisspalte(kategorie);
                        var felder = g.Felder.Where(f => f.Kategorie == kategorie).ToList();
                        // (a) Keine Zeile mit Kosten ohne Verweis - ausgenommen ein Stueckpreis ohne Baugroesse.
                        foreach (Katalogkostenfeld f in felder.Where(x => string.IsNullOrEmpty(x.Bezugsspalte)))
                            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE COALESCE(\"" +
                                                  f.Spalte + "\",0) > 0 AND \"" + verweis + "\" IS NULL"));
                        // (b) Jeder Verweis zeigt auf GENAU EINE Vorlage des Gewerks und der Art.
                        long falsch = Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" t WHERE t.\"" + verweis +
                                           "\" IS NOT NULL AND NOT EXISTS (SELECT 1 FROM \"" +
                                           KatalogkostenSaatSchema.TAB_KOSTENVORLAGE + "\" v WHERE v.\"ID\" = t.\"" +
                                           verweis + "\" AND v.\"KomponentenID\" = " + g.KomponentenId +
                                           " AND v.\"KategorieID\" = " + kategorie + ")");
                        Assert.Equal(0L, falsch);
                        // (c) Keine Zeile OHNE Kosten bekommt eine Vorlage.
                        string ohne = string.Join(" AND ", felder.Select(f => "COALESCE(\"" + f.Spalte + "\",0) <= 0"));
                        if (felder.Count > 0)
                            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE " + ohne +
                                                  " AND \"" + verweis + "\" IS NOT NULL"));
                        gesaet += (int)Zahl("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + verweis + "\" IS NOT NULL");
                    }
            Assert.True(gesaet > 0, "Die Testdatenbank fuehrt gepflegte Geraetekosten; die Saat muss greifen.");

            // Je Satzvorlage mindestens eine Position, und kein Satz traegt zwei Vorlagen derselben Art.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGE + "\" v " +
                                  "WHERE v.\"Bemerkung\" LIKE 'Aus den Kostenspalten%' AND NOT EXISTS " +
                                  "(SELECT 1 FROM \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGEPOSITION +
                                  "\" p WHERE p.\"VorlageID\" = v.\"ID\")"));
        }

        [Fact]
        public void Ein_zweiter_Lauf_aendert_nichts()
        {
            if (!_db.Vorhanden) return;
            long vorlagen = Zahl("SELECT COUNT(*) FROM \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGE + "\"");
            long positionen = Zahl("SELECT COUNT(*) FROM \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGEPOSITION + "\"");
            var bericht = new List<string>();
            Assert.Equal(0, KatalogkostenSaatSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains("steht bereits", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("nichts zu tun", StringComparison.Ordinal));
            Assert.Equal(vorlagen, Zahl("SELECT COUNT(*) FROM \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGE + "\""));
            Assert.Equal(positionen, Zahl("SELECT COUNT(*) FROM \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGEPOSITION + "\""));
        }

        [Fact]
        public void Der_Satz_eines_Stueckpreises_trifft_wieder_den_Geraetepreis()
        {
            if (!_db.Vorhanden) return;
            // Photovoltaik: Satz [EUR/kWp] x (Leistung [W] / 1000) = Modulkosten je Modul.
            Pruefe("Tab_PV_STAMM", "Modulkosten", "Leistung", 1000.0, 3, DbWerte.BEMESSUNG_EUR_PRO_KWP);
            Pruefe("Tab_PV", "Modulkosten", "Leistung", 1000.0, 3, DbWerte.BEMESSUNG_EUR_PRO_KWP);
            // Solarthermie: Satz [EUR/m2] x Aperturflaeche = Investitionskosten je Kollektor.
            Pruefe("Tab_Solarkollektoren_STAMM", "Investitionskosten", "Aperturflaeche", 1.0, 4,
                   DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR);
            Pruefe("Tab_Solarkollektoren", "Investitionskosten", "Aperturflaeche", 1.0, 4,
                   DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR);
        }

        [Fact]
        public void Der_abgeleitete_BHKW_Wert_je_kWel_wird_nicht_gesaet()
        {
            // Investition_kwel ist aus Kosten_Modul und den vier Nebenposten abgeleitet (BHKWKosten);
            // eine Position daraus waere dieselbe Zahl ein zweites Mal.
            Katalogkostengewerk bhkw = KatalogkostenSaatSchema.GEWERKE.Single(g => g.KomponentenId == 7);
            Assert.DoesNotContain(bhkw.Felder, f => f.Spalte == "Investition_kwel");
            Assert.Contains(bhkw.Felder, f => f.Spalte == "Kosten_Modul");
            Assert.Equal(6, bhkw.Felder.Count);
        }

        private static void Pruefe(string tabelle, string kostenspalte, string bezugsspalte,
                                    double teiler, int komponentenId, string bemessung)
        {
            string verweis = KatalogkostenSaatSchema.Verweisspalte(DbWerte.KOSTEN_KATEGORIE_INVESTITION);
            var dt = DataRepository.GetDataTable(
                "SELECT t.\"" + kostenspalte + "\" AS kosten, t.\"" + bezugsspalte + "\" AS bezug, p.\"Satz\" AS satz, " +
                "p.\"Bemessung\" AS bem FROM \"" + tabelle + "\" t " +
                "JOIN \"" + KatalogkostenSaatSchema.TAB_KOSTENVORLAGEPOSITION + "\" p ON p.\"VorlageID\" = t.\"" + verweis + "\" " +
                "WHERE t.\"" + verweis + "\" IS NOT NULL AND COALESCE(t.\"" + kostenspalte + "\",0) > 0");
            Assert.NotNull(dt);
            foreach (System.Data.DataRow r in dt.Rows)
            {
                double kosten = Convert.ToDouble(r["kosten"], CultureInfo.InvariantCulture);
                double bezug = Convert.ToDouble(r["bezug"], CultureInfo.InvariantCulture) / teiler;
                double satz = Convert.ToDouble(r["satz"], CultureInfo.InvariantCulture);
                Assert.Equal(bemessung, Convert.ToString(r["bem"], CultureInfo.InvariantCulture));
                Assert.True(Math.Abs(satz * bezug - kosten) < 1e-6,
                            tabelle + ": Satz " + satz + " x " + bezug + " trifft " + kosten + " nicht.");
            }
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static List<(string Spalte, string Ziel)> Verweise(string tabelle)
        {
            var liste = new List<(string, string)>();
            var dt = DataRepository.GetDataTable("SELECT \"from\", \"table\" FROM pragma_foreign_key_list(?)",
                                                 new DbParam("@t", tabelle));
            foreach (System.Data.DataRow r in dt.Rows)
                liste.Add((Convert.ToString(r[0], CultureInfo.InvariantCulture),
                           Convert.ToString(r[1], CultureInfo.InvariantCulture)));
            return liste;
        }
    }
}
