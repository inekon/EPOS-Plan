using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KBP-5a — Schemaschritt <see cref="BedarfsprofilEingabeartSchema"/></b> auf einer frisch migrierten Kopie der
    /// Testdatenbank: die (geparkte) Nummer, die vier Spalten samt Prüfklausel und Vorgabe, die Saat allein an Vorlage 8,
    /// die Zeilen 1–6 unberührt samt Prüfsumme, die Wiederholbarkeit und dass eine Anwenderangabe stehen bleibt.
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfsprofilEingabeartSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Prüfsummen der Zeilen 1–6 des Kältekatalogs, eingefroren vor KBP-2 (dieselben wie dort).</summary>
        private static readonly (long Id, string Pruefsumme)[] ABDRUCK_1_BIS_6 =
        {
            (1, "a64f424ada0aab876615e7f3d92519d18185d1d593a8128ed5477f271f0cc218"),
            (2, "295c04a8ca1650d6ab87c935e8d2bdfd0969e4e489384fa56e0831c5bb96af09"),
            (3, "9ffe94c20b37989e181feea0f35ce55cd58c8e66cc9d5bc1dfbc10e8924e2887"),
            (4, "ecd764c5ae2b35170681d521ff516f74450c80db435288aeacdbd1aa8beb4c0a"),
            (5, "b494a5e15b9ef9e728ef9b2c3a66174be9e7c76ee081c616679a5646c336c79b"),
            (6, "e98c1f14381f198ca1cf648d44dbc4b1ab7babe2faba70617a17294a928b466b"),
        };

        /// <summary>Die Prüfsumme der Vorlage 8 VOR dem Schritt (Testdatenbank 230).</summary>
        private const string PRUEFSUMME_8_VORHER = "7cc242baa95c40710758452690fb7a467848a6d82c497795533e821928a45509";

        [Fact]
        public void Nummer_geparkt_und_noch_nicht_im_Register()
        {
            // KETTE: geparkt auf 234; nach dem Umhaengen hinter 233 BauteilUHerkunftSchema gelten statt der drei letzten
            // Zeilen: SCHRITT == BauteilUHerkunftSchema.SCHRITT + 1, Zielversion == SCHRITT, Stufe in der Paketanhebung.
            Assert.Equal(234, BedarfsprofilEingabeartSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion < BedarfsprofilEingabeartSchema.SCHRITT);
            Assert.DoesNotContain(Paketanhebung.Stufen, s => s.Nr == BedarfsprofilEingabeartSchema.SCHRITT);
            Assert.Equal(4, BedarfsprofilEingabeartSchema.SPALTEN.Count);
            Assert.Contains(BedarfsprofilEingabeartSchema.SPALTE_NUTZUNGSTAGE,
                            Katalogfassung.Tabelle(KaeltebedarfSchema.TAB_KOPF_STAMM).Fachspalten);
        }

        [Fact]
        public void Spalten_stehen_mit_Vorgabe_jahr_und_ohne_Update_des_Bestands()
        {
            if (!_db.Vorhanden) return;
            Assert.True(BedarfsprofilEingabeartSchema.SchemaVollstaendig());
            Assert.True(BedarfsprofilEingabeartSchema.Vollstaendig());
            foreach (string tab in new[] { BedarfsprofilEingabeartSchema.TAB_PROZESS, BedarfsprofilEingabeartSchema.TAB_BRAUCHWASSER })
            {
                Assert.True(Zahl("SELECT COUNT(*) FROM \"" + tab + "\"") > 0, tab + " ist leer.");
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"" + tab + "\" WHERE \"Eingabeart\" IS NOT 'jahr'"));
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@n", tab)), CultureInfo.InvariantCulture);
                Assert.Contains("\"Eingabeart\" TEXT NOT NULL DEFAULT 'jahr' CHECK (\"Eingabeart\" IN ('jahr','monat','woche','typtag'))", ddl);
            }
        }

        [Fact]
        public void Pruefklauseln_weisen_falsche_Werte_ab()
        {
            if (!_db.Vorhanden) return;
            long idPw = Zahl("SELECT MIN(ID) FROM Z_Projekt_Prozesswaerme");
            Assert.True(DataRepository.ExecuteSQL("UPDATE Z_Projekt_Prozesswaerme SET \"Eingabeart\" = ? WHERE ID = ?",
                                                  new DbParam("@e", "woche"), new DbParam("@id", idPw)));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Z_Projekt_Prozesswaerme SET \"Eingabeart\" = 'frei' WHERE ID = " + idPw));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Z_Projekt_Brauchwasser SET \"Eingabeart\" = NULL"));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltebedarf_STAMM SET \"Nutzungstage_Norm\" = 0 WHERE ID = 1"));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Kaeltebedarf_STAMM SET \"Nutzungstage_Norm\" = 366 WHERE ID = 1"));
            Assert.Equal(365, BedarfsprofilEingabeartSchema.Nutzungstage(365L));
            Assert.Null(BedarfsprofilEingabeartSchema.Nutzungstage(0L));
            Assert.Null(BedarfsprofilEingabeartSchema.Nutzungstage(DBNull.Value));
        }

        [Fact]
        public void Saat_allein_an_Vorlage_8_mit_neuer_Pruefsumme()
        {
            if (!_db.Vorhanden) return;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Kaeltebedarf_STAMM ORDER BY ID", null);
            foreach (DataRow r in dt.Rows)
            {
                long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                object tage = r[BedarfsprofilEingabeartSchema.SPALTE_NUTZUNGSTAGE];
                if (id == BedarfsprofilEingabeartSchema.SAAT_ID)
                {
                    Assert.Equal(BedarfsprofilEingabeartSchema.SAAT_SCHLUESSEL, r["Katalog_Schluessel"]);
                    Assert.Equal(230L, Convert.ToInt64(tage, CultureInfo.InvariantCulture));
                }
                else Assert.Equal(DBNull.Value, tage);
            }
            Assert.Equal(230, KaeltebedarfStammCtrl.Nutzungstage("Prozesskälte einschichtig"));
            Assert.Null(KaeltebedarfStammCtrl.Nutzungstage("Raumkühlung Büro"));

            Katalogtabelle t = Katalogfassung.Tabelle(KaeltebedarfSchema.TAB_KOPF_STAMM);
            List<string> spalten = t.Fachspalten.ToList();
            DataTable z = DataRepository.GetDataTable("SELECT ID, Katalog_Pruefsumme, " + Katalogfassung.Spaltentext(spalten) +
                                                      " FROM Tab_Kaeltebedarf_STAMM WHERE ID <= 8 ORDER BY ID", null);
            foreach (DataRow r in z.Rows)
                Assert.Equal(r["Katalog_Pruefsumme"], Katalogfassung.PruefsummeDerZeile(t, spalten, r, Katalogfassung.LeseOhneVorgang));
            for (int i = 0; i < ABDRUCK_1_BIS_6.Length; i++)
                Assert.Equal(ABDRUCK_1_BIS_6[i].Pruefsumme, z.Rows[i]["Katalog_Pruefsumme"]);
            Assert.NotEqual(PRUEFSUMME_8_VORHER, z.Rows.Cast<DataRow>().Single(r => Convert.ToInt64(r["ID"]) == 8)["Katalog_Pruefsumme"]);
        }

        [Fact]
        public void Projektkopie_traegt_die_Nutzungstage()
        {
            if (!_db.Vorhanden) return;
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm("Prozesskälte einschichtig", 1017);
            Assert.True(kopie > 0);
            Assert.Equal(230, KaeltebedarfStammCtrl.ProjektNutzungstage(kopie));
            Assert.Null(KaeltebedarfStammCtrl.ProjektNutzungstage(KaeltebedarfStammCtrl.CopyFromStamm("Raumkühlung Büro", 1017)));
        }

        [Fact]
        public void Wiederholbar_und_eine_Anwenderangabe_bleibt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Kaeltebedarf_STAMM SET \"Nutzungstage_Norm\" = ? WHERE ID = ?",
                                                  new DbParam("@t", 200), new DbParam("@id", 8)));
            var bericht = new List<string>();
            Assert.Equal(0, BedarfsprofilEingabeartSchema.Ausfuehren(bericht));
            Assert.Equal(200L, Zahl("SELECT \"Nutzungstage_Norm\" FROM Tab_Kaeltebedarf_STAMM WHERE ID = 8"));
            Assert.Contains(bericht, b => b.Contains("stehen bereits", StringComparison.Ordinal));
        }

        private static void Ausfuehren(string sql)
        {
            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren(sql);
            v.Commit();
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
