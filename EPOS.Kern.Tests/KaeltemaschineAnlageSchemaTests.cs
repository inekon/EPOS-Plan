using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KU3-4 — Schemaschritt 183: die Kältemaschine als Anlage</b> (<see cref="KaeltemaschineAnlageSchema"/>):
    /// Nummer, Prüfklauseln, Typ, Spalten, Kostenkomponente samt Vorlagen und Nutzungsdauer, Ergebnistabelle,
    /// Wiederholbarkeit.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineAnlageSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_Nummer_folgt_lueckenlos_und_die_Paketanhebung_fuehrt_DDL()
        {
            Assert.Equal(KaeltemaschineSchema.SCHRITT + 1, KaeltemaschineAnlageSchema.SCHRITT);
            Assert.Equal(183, KaeltemaschineAnlageSchema.SCHRITT);
            Assert.Equal(KaeltemaschineAnlageSchema.SCHRITT + 1, KaeltestromabrechnungSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KaeltemaschineAnlageSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl, Paketanhebung.Stufen.Single(x => x.Nr == KaeltemaschineAnlageSchema.SCHRITT).Wirkung);
            Assert.Equal(13, KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE);
            Assert.Equal(11, KaeltemaschineAnlageSchema.KOMPONENTE_KAELTEMASCHINE);
            // Die feste Typnummer ist keine der Wärmeseite.
            Assert.DoesNotContain(KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE, new[]
            {
                WizardItemClass.WP_TYP, WizardItemClass.SOLAR_TYP, WizardItemClass.PV_TYP, WizardItemClass.SP_TYP,
                WizardItemClass.KESSEL_TYP, WizardItemClass.BHKW_TYP, WizardItemClass.PUFFER_TYP
            });
            Assert.Equal(15, KaeltemaschineAnlageSchema.NUTZUNGSDAUER_JAHRE);
        }

        [Fact]
        public void Die_Pruefklauseln_halten_Anzahl_Hilfsstrom_und_den_Verweis()
        {
            using var c = new SqliteConnection("Data Source=:memory:");
            c.Open();
            Ausfuehren(c, "PRAGMA foreign_keys = ON");
            Ausfuehren(c, "CREATE TABLE \"Tab_Projekt\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_Ergebnis\" (ID INTEGER PRIMARY KEY) STRICT");
            Ausfuehren(c, "CREATE TABLE \"Tab_Energieanlagen\" (ID INTEGER PRIMARY KEY, Bezeichner TEXT) STRICT");
            Ausfuehren(c, "INSERT INTO Tab_Projekt (ID) VALUES (1)");
            Ausfuehren(c, "INSERT INTO Tab_Ergebnis (ID) VALUES (1)");
            foreach (var t in KaeltemaschineSchema.Tabellen()) Ausfuehren(c, t.Value);
            foreach (var s in KaeltemaschineAnlageSchema.SPALTEN) Ausfuehren(c, KaeltemaschineAnlageSchema.Anlegen(s));
            Ausfuehren(c, KaeltemaschineAnlageSchema.SqlCreateErgebnis());
            Assert.Contains("STRICT", (string)Skalar(c, "SELECT sql FROM sqlite_master WHERE name = 'Tab_ErgebnisKaeltemaschine'"));

            Ausfuehren(c, "INSERT INTO Tab_Kaeltemaschine (ID, ID_Projekt, Bezeichner) VALUES (1, 1, 'A')");
            Ausfuehren(c, "INSERT INTO Tab_Energieanlagen (ID, Bezeichner, ID_Kaeltemaschine) VALUES (1, 'KM', 1)");
            Assert.Equal(1L, Skalar(c, "SELECT Kaeltemaschine_Anzahl FROM Tab_Energieanlagen WHERE ID = 1"));
            Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET Kaeltemaschine_Anzahl = 0"));
            Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET Kaeltemaschine_Anzahl = NULL"));
            Assert.True(Wirft(c, "UPDATE Tab_Energieanlagen SET ID_Kaeltemaschine = 99"));
            Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine SET Kuehl_Hilfsstromanteil = 1"));
            Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine SET Kuehl_Hilfsstromanteil = -0.1"));
            Assert.False(Wirft(c, "UPDATE Tab_Kaeltemaschine SET Kuehl_Hilfsstromanteil = 0.05, Kuehl_Vorlauf = 7"));
            Assert.True(Wirft(c, "UPDATE Tab_Kaeltemaschine SET Kuehl_Vorlauf = 40"));

            Ausfuehren(c, "INSERT INTO Tab_ErgebnisKaeltemaschine (ID_Ergebnis, ID_Kaeltemaschine, Kaelteproduktion_MWh) VALUES (1, 1, 2.5)");
            Assert.True(Wirft(c, "INSERT INTO Tab_ErgebnisKaeltemaschine (ID_Ergebnis, Taktstunden) VALUES (1, 'viele')"));
            // Löschen der Projektkopie: die Anlagenzeile und das Ergebnis behalten ihre Zeile, der Verweis fällt auf NULL.
            Ausfuehren(c, "DELETE FROM Tab_Kaeltemaschine WHERE ID = 1");
            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Kaeltemaschine IS NULL"));
            Assert.Equal(1L, Skalar(c, "SELECT COUNT(*) FROM Tab_ErgebnisKaeltemaschine WHERE ID_Kaeltemaschine IS NULL"));
            Ausfuehren(c, "DELETE FROM Tab_Ergebnis WHERE ID = 1");
            Assert.Equal(0L, Skalar(c, "SELECT COUNT(*) FROM Tab_ErgebnisKaeltemaschine"));
        }

        [Fact]
        public void Die_Testdatenbank_steht_auf_dem_Schritt_und_er_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaeltemaschineAnlageSchema.Vollstaendig());
            Assert.Equal("Kältemaschine", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_Typ_Energieanlagen WHERE ID = ?", new DbParam("?", 13))));
            Assert.Equal(DbWerte.KOSTEN_KOMPONENTE_KAELTEMASCHINE, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Komponente FROM Tab_KostenKomponente WHERE ID = ?", new DbParam("?", 11))));
            Assert.Equal(15.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Nutzungsdauer_a FROM Tab_Nutzungsdauer WHERE KomponentenID = ? AND IstStandard = 1", new DbParam("?", 11))));
            Assert.Equal(KaeltemaschineAnlageSchema.SATZ_AGGREGAT_EUR_JE_KW, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT p.Satz FROM Tab_KostenVorlagePosition p JOIN Tab_KostenVorlage v ON v.ID = p.VorlageID " +
                "WHERE v.KomponentenID = ? AND v.KategorieID = ? AND p.Bezeichnung = ?",
                new DbParam("?", 11), new DbParam("?", DbWerte.KOSTEN_KATEGORIE_INVESTITION),
                new DbParam("?", KaeltemaschineAnlageSchema.POSITION_AGGREGAT))));
            // Schritt 184 (KU3-4d) haengt seine Abrechnungsspalten an dieselbe Tabelle.
            // + fuenf Kennzahlspalten von Teillast und Takten (KaeltemaschineTeillastSchema, Schritt 210).
            // + sechs Kennzahlen der Rueckkuehlung (RueckkuehlwerkSchema, K-F1).
            Assert.Equal(4 + KaeltemaschineAnlageSchema.ErgebnisSpalten.Length + KaeltestromabrechnungSchema.SPALTEN.Count +
                         KaeltemaschineTeillastSchema.ERGEBNIS_SPALTEN.Count + RueckkuehlwerkSchema.SPALTEN_ERGEBNIS.Count,
                         DataRepository.SpaltenVonTabelle(KaeltemaschineAnlageSchema.TAB_ERGEBNIS).Count);
            // Eine Anlagenzeile der Kältemaschine führen allein die Referenzprojekte 1055 (KU3-4b), 1059 (AK3-K-K5a) und 1063 (KM3).
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE (ID_Kaeltemaschine IS NOT NULL OR ID_Type = ?) AND ID_Projekt NOT IN (1055, 1059, 1063)", new DbParam("?", 13))));
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Kaeltemaschine_Anzahl <> 1")));

            // Wiederholt: keine Anweisung, keine neue Zeile.
            Assert.Equal(0, KaeltemaschineAnlageSchema.Ausfuehren(null));
            Assert.Equal(0, KaeltemaschineAnlageSchema.Saat(null));
        }

        /// <summary>
        /// Die Positionen der Vorlagen verweisen nach der Saat-Zuordnung: Aggregat auf „Gerät", Montage und Planung auf
        /// die technikübergreifenden Zeilen, Rückkühlung, MSR und jede Betriebsposition auf keine. Eine Datenbank aus der
        /// ersten Fassung des Schritts (jede Position auf „Gerät") wird beim nächsten Lauf nachgetragen, ein dritter Lauf
        /// findet nichts mehr.
        /// </summary>
        [Fact]
        public void Die_Positionen_tragen_ihre_Positionsart_und_der_Nachtrag_heilt_die_erste_Fassung()
        {
            if (!_db.Vorhanden) return;
            const string verweise =
                "SELECT p.Bezeichnung || '=' || IFNULL(n.Positionsart, '-') FROM Tab_KostenVorlagePosition p " +
                "JOIN Tab_KostenVorlage v ON v.ID = p.VorlageID LEFT JOIN Tab_Nutzungsdauer n ON n.ID = p.NutzungsdauerID " +
                "WHERE v.KomponentenID = 11 AND v.IstStandard = 1 ORDER BY v.KategorieID, p.Sortierung";
            string[] soll =
            {
                KaeltemaschineAnlageSchema.POSITION_AGGREGAT + "=Gerät", "Rückkühlung / Zubehör=-", "MSR-Technik / Automation=-",
                "Montage, Installation & Kältetechnik=Montage", "Planung / Baunebenkosten=Planung / Baunebenkosten",
                "Wartung Kältemaschine=-", "Instandhaltung Kältemaschine=-",
            };
            string[] Ist() => DataRepository.GetDataTable(verweise).Rows.Cast<System.Data.DataRow>()
                .Select(r => Convert.ToString(r[0])).ToArray();
            Assert.Equal(soll, Ist());

            // Die erste Fassung: jede Position verweist auf die Gerätezeile.
            DataRepository.ExecuteSQL(
                "UPDATE Tab_KostenVorlagePosition SET NutzungsdauerID = (SELECT ID FROM Tab_Nutzungsdauer " +
                "WHERE KomponentenID = 11 AND IstStandard = 1) WHERE VorlageID IN " +
                "(SELECT ID FROM Tab_KostenVorlage WHERE KomponentenID = 11 AND IstStandard = 1)");
            Assert.Equal(6, KaeltemaschineAnlageSchema.Saat(null));
            Assert.Equal(soll, Ist());
            Assert.Equal(0, KaeltemaschineAnlageSchema.Saat(null));
        }

        private static void Ausfuehren(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        private static object Skalar(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteScalar();
        }

        private static bool Wirft(SqliteConnection c, string sql)
        {
            try { Ausfuehren(c, sql); return false; }
            catch (SqliteException) { return true; }
        }
    }
}
