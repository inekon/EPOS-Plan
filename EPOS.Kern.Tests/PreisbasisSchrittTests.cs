using System;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>Schritt F (Schemaschritt 112): die Preisbasis der Trägerkarte als
    /// eigener Kartenzustand</b> (Entscheid ET‑D‑3 Rest, Mockup U32).
    ///
    /// <para>Geprüft wird: Zielstand, Spalte (TEXT, nullbar), der einmalige Datenteil
    /// aus <c>ID_Umrechnung</c> (Regel nach kWh → „kWh", sonst die Abrechnungseinheit —
    /// gemessen an der Testdatenbank: 5 Zeilen über die Regel, 23 über die
    /// Abrechnungseinheit, danach 6 × kWh, 17 × Nm³, 4 × L, 1 × kg), seine
    /// Wiederholbarkeit, und der Lese-/Schreibweg des Controllers, der die Basis „kWh"
    /// auch ohne Regel nach kWh hält (die Abnahme von U32).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class PreisbasisSchrittTests
    {
        [Fact]
        public void Der_Zielstand_ist_112_und_der_Schritt_hat_eine_Textspalte()
        {
            Assert.True(SchemaStand.Zielversion >= 112,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 112.");

            SchemaSpalte[] spalten = SchemaKatalog.Schritt112_Preisbasis;
            Assert.Single(spalten);
            Assert.Equal("energy_project_settings", spalten[0].Tabelle);
            Assert.Equal("Preisbasis", spalten[0].Name);
            Assert.Equal("TEXT", StilleDb.SqliteSpaltenTyp(spalten[0].Name, spalten[0].TypDefinition));
        }

        /// <summary>Die nachgezogene Arbeitskopie: Spalte nullbar, jede Zeile mit
        /// Abrechnungseinheit trägt ihre Basis, und die Verteilung ist die gemessene.</summary>
        [Fact]
        public void Der_Datenteil_setzt_die_Basis_aus_der_Umrechnungsregel()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(DataRepository.SpalteVorhanden("energy_project_settings", "Preisbasis"));
            Assert.Equal(0, Zahl("SELECT \"notnull\" FROM pragma_table_info('energy_project_settings') " +
                                 "WHERE name = 'Preisbasis'"));
            Assert.Equal(0, PreisbasisUebernahme.Offen());

            // Mit dem Referenzprojekt der Anlagenkopplung 1047 (Kopie von 1017) zwei Zeilen „kWh"
            // und eine „kg" mehr - die drei Trägerzeilen von 1017. ETAPPE E24 (§ 6.3 Nr. 24): eine
            // Zeile „Nm³" mehr - die Erdgaszeile von 1023 (Kopie der Zeile von 1030). Das Prüfprojekt
            // PV mit Preisen 1048: je eine Zeile „kWh" (Strom) und „Nm³" (Erdgas, Kopie von 1040).
            // Das Referenzprojekt Solarthermie 1049: eine Zeile „Nm³" (Erdgas, Kopie von 1018).
            Assert.Equal(9, Zahl("SELECT COUNT(*) FROM energy_project_settings WHERE Preisbasis = 'kWh'"));
            Assert.Equal(20, Zahl("SELECT COUNT(*) FROM energy_project_settings WHERE Preisbasis = 'Nm³'"));
            Assert.Equal(4, Zahl("SELECT COUNT(*) FROM energy_project_settings WHERE Preisbasis = 'L'"));
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM energy_project_settings WHERE Preisbasis = 'kg'"));

            // Die Regel nach kWh (51: kWh → kWh am Stromträger) trägt „kWh", die
            // Identitätsregel des Erdgases die Abrechnungseinheit.
            Assert.Equal("kWh", Text("SELECT Preisbasis FROM energy_project_settings WHERE ID_Projekt = 1024 AND [ID_Energieträger] = 60"));
            Assert.Equal("Nm³", Text("SELECT Preisbasis FROM energy_project_settings WHERE ID_Projekt = 1030 AND [ID_Energieträger] = 63"));

            // Wiederholbar: Der zweite Lauf trifft nichts.
            PreisbasisUebernahme.Bericht zweiter = PreisbasisUebernahme.Ausfuehren();
            Assert.Equal(0, zweiter.Kwh);
            Assert.Equal(0, zweiter.Abrechnungseinheit);
        }

        /// <summary>Eine leere Zeile, deren Regel nach kWh zeigt (Erdgas E, Regel 67
        /// Nm³ → kWh), bekommt „kWh" — auch wenn die Abrechnungseinheit Nm³ ist.</summary>
        [Fact]
        public void Eine_Regel_nach_kWh_ergibt_die_Basis_kWh()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE energy_project_settings SET ID_Umrechnung = 67, Preisbasis = NULL " +
                "WHERE ID_Projekt = 1030 AND [ID_Energieträger] = 63");
            PreisbasisUebernahme.Bericht b = PreisbasisUebernahme.Ausfuehren();

            Assert.Equal(1, b.Kwh);
            Assert.Equal(0, b.Abrechnungseinheit);
            Assert.Equal("kWh", Text("SELECT Preisbasis FROM energy_project_settings WHERE ID_Projekt = 1030 AND [ID_Energieträger] = 63"));
        }

        /// <summary>
        /// <b>Die Abnahme von U32 am Controller:</b> Speichern mit der Basis „kWh" OHNE
        /// Regel nach kWh (ID_Umrechnung −1 → NULL) und wieder lesen — die Basis steht
        /// weiter auf kWh; vor Schritt 112 fiel sie auf die Abrechnungseinheit zurück.
        /// </summary>
        [Fact]
        public void Die_Basis_kWh_bleibt_ohne_Regel_nach_kWh_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            const int projekt = 1024, stadtgas = 64;
            EnergietraegerPreisCtrl.Projektpreis vorher = EnergietraegerPreisCtrl.ProjektpreisLesen(projekt, stadtgas);
            Assert.NotNull(vorher);
            Assert.False(vorher.PreisbasisSpalteFehlt);

            EnergietraegerPreisCtrl.Projektwerte(projekt, stadtgas, new EnergietraegerPreisCtrl.Preisstand
            {
                Arbeitspreis = vorher.Arbeitspreis ?? 0,
                Grundpreis = vorher.Grundpreis ?? 0,
                Leistungspreis = vorher.Leistungspreis ?? 0,
                Hi = vorher.Hi ?? 0,
                Hs = vorher.Hs ?? 0,
                CO2 = vorher.CO2 ?? 0,
                SO2 = vorher.SO2 ?? 0,
                NOx = vorher.NOx ?? 0,
                IdUmrechnung = -1,
                Preisbasis = "kWh"
            });

            EnergietraegerPreisCtrl.Projektpreis nachher = EnergietraegerPreisCtrl.ProjektpreisLesen(projekt, stadtgas);
            Assert.Equal("kWh", nachher.Preisbasis);
            Assert.Null(nachher.IdUmrechnung);
            Assert.Equal(vorher.Arbeitspreis, nachher.Arbeitspreis);
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        private static string Text(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? null : Convert.ToString(o);
        }
    }
}
