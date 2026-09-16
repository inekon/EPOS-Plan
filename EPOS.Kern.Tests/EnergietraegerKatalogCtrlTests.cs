using System;
using System.Data;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die fuenf SCHREIBWEGE der Energietraegerverwaltung auf einer Arbeitskopie der
    /// Testdatenbank — <c>Umbenennen</c>, <c>Neu</c>, <c>Variante</c>, <c>Loeschen</c> und
    /// <c>AusProjektEntfernen</c>.
    ///
    /// <para><b>Warum es diese Faelle gibt (Testloch ET-5, 16.09.2026).</b> Alle fuenf
    /// standen bis hierher OHNE Pruefung da: Der Referenzlauf rechnet einen bestehenden
    /// Projektstand nach, er legt keinen Traeger an und entfernt keinen; die bunit-Faelle
    /// des Dialogs reichen Attrappen herein. Damit war die Katalogpflege allein am
    /// Windows-Geraet nachweisbar — und der Anwenderbefund vom 16.09.2026 („der entfernte
    /// Traeger bleibt in der Liste stehen") traf auf keine einzige rote Probe.</para>
    ///
    /// <para>Die Faelle arbeiten auf der ARBEITSKOPIE (<see cref="TestDatenbank"/>); die
    /// Quelldatei ist die Messlatte der Referenzlaeufe und bleibt unberuehrt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergietraegerKatalogCtrlTests
    {
        /// <summary>Erdgas E — Katalogzeile, die mehrere Projekte fuehren.</summary>
        private const int ERDGAS_E = 63;

        /// <summary>Projekt 1024; es fuehrt acht Traegerzuordnungen und eine elektrische Welt.</summary>
        private const int PROJEKT = 1024;

        /// <summary>„Heizoel L" — 1024 zugeordnet, von KEINER Anlage gehalten.</summary>
        private const int OHNE_ANLAGE = 62;

        /// <summary>„Heizoel L var" — die Kesselanlage 11257 des Projekts haelt ihn.</summary>
        private const int MIT_ANLAGE = 71;

        /// <summary>„Elektrische Energie" — der Stromtraeger, den die Waermepumpe beitraegt.</summary>
        private const int STROM = 60;

        private static string Name() => "Pruefträger " + Guid.NewGuid().ToString("N").Substring(0, 8);

        private static string Text(int carrierId, string spalte)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [" + spalte + "] FROM energy_carrier WHERE id = ?",
                new DbParam("@id", carrierId));
            return o == null || o == DBNull.Value ? null : Convert.ToString(o);
        }

        private static int Zaehle(string sql, params DbParam[] p)
        {
            object o = DataRepository.ExecuteScalar(sql, p);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }

        // =================================================================================
        // Umbenennen
        // =================================================================================

        [Fact]
        public void Umbenennen_setzt_Bezeichnung_und_Gruppe_und_laesst_den_Code_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string code = Text(ERDGAS_E, "code");
            string neu = Name();

            Assert.True(EnergietraegerKatalogCtrl.Umbenennen(ERDGAS_E, neu, "Prüfgruppe"));

            Assert.Equal(neu, Text(ERDGAS_E, "name"));
            Assert.Equal("Prüfgruppe", Text(ERDGAS_E, "group_code"));
            // Der technische Code ist Verweisanker, kein Anzeigename — er bleibt stehen.
            Assert.Equal(code, Text(ERDGAS_E, "code"));
        }

        [Fact]
        public void Umbenennen_lehnt_eine_leere_Bezeichnung_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string vorher = Text(ERDGAS_E, "name");

            Assert.False(EnergietraegerKatalogCtrl.Umbenennen(ERDGAS_E, "   ", "Gas"));
            Assert.False(EnergietraegerKatalogCtrl.Umbenennen(0, "Irgendwas", "Gas"));

            Assert.Equal(vorher, Text(ERDGAS_E, "name"));
        }

        // =================================================================================
        // Neu
        // =================================================================================

        [Fact]
        public void Neu_legt_einen_Traeger_mit_neutraler_Vorbelegung_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = Name();
            int id = EnergietraegerKatalogCtrl.Neu(name, "Prüfgruppe");

            Assert.True(id > 0);
            Assert.Equal(name, Text(id, "name"));
            Assert.Equal("Prüfgruppe", Text(id, "group_code"));
            Assert.Equal("GASEOUS_FUEL", Text(id, "pricing_model"));
            Assert.Equal("kWh", Text(id, "billing_unit"));
            // kWh mit Faktor 1: der Traeger erreicht kWh vom ersten Augenblick an.
            Assert.Equal(1, Zaehle("SELECT COUNT(*) FROM energy_carrier " +
                                   "WHERE id = ? AND hi_kwh_per_unit = 1 AND hs_kwh_per_unit = 1",
                                   new DbParam("@id", id)));
        }

        [Fact]
        public void Neu_ohne_Namen_legt_nichts_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int vorher = Zaehle("SELECT COUNT(*) FROM energy_carrier");

            Assert.Equal(0, EnergietraegerKatalogCtrl.Neu("", null));
            Assert.Equal(0, EnergietraegerKatalogCtrl.Neu("   ", "Gas"));

            Assert.Equal(vorher, Zaehle("SELECT COUNT(*) FROM energy_carrier"));
        }

        // =================================================================================
        // Variante
        // =================================================================================

        [Fact]
        public void Variante_kopiert_die_Katalogzeile_bis_auf_Id_Name_und_Code()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataTable quelle = DataRepository.GetDataTable(
                "SELECT * FROM energy_carrier WHERE id = ?", new DbParam("@id", ERDGAS_E));
            Assert.NotNull(quelle);
            DataRow q = quelle.Rows[0];

            int id = EnergietraegerKatalogCtrl.Variante(ERDGAS_E);
            Assert.True(id > 0);
            Assert.NotEqual(ERDGAS_E, id);

            DataTable kopie = DataRepository.GetDataTable(
                "SELECT * FROM energy_carrier WHERE id = ?", new DbParam("@id", id));
            DataRow k = kopie.Rows[0];

            Assert.Equal(Convert.ToString(q["name"]) + " Variante", Convert.ToString(k["name"]));
            Assert.NotEqual(Convert.ToString(q["code"]), Convert.ToString(k["code"]));

            // Jede ANDERE Spalte ist wortgleich — genau dafuer ist die Variante da: je
            // Traeger abweichende Emissionswerte oder Preise als eigener Eintrag.
            foreach (DataColumn sp in quelle.Columns)
            {
                string s = sp.ColumnName;
                if (string.Equals(s, "id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s, "name", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s, "code", StringComparison.OrdinalIgnoreCase)) continue;
                Assert.Equal(Convert.ToString(q[s]), Convert.ToString(k[s]));
            }
        }

        [Fact]
        public void Eine_zweite_Variante_bekommt_die_naechste_freie_Nummer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string basis = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT [name] FROM energy_carrier WHERE id = ?", new DbParam("@id", ERDGAS_E)));

            int erste = EnergietraegerKatalogCtrl.Variante(ERDGAS_E);
            int zweite = EnergietraegerKatalogCtrl.Variante(ERDGAS_E);

            Assert.True(erste > 0 && zweite > 0);
            Assert.Equal(basis + " Variante", Text(erste, "name"));
            Assert.Equal(basis + " Variante 2", Text(zweite, "name"));
        }

        // =================================================================================
        // Loeschen
        // =================================================================================

        [Fact]
        public void Loeschen_entfernt_einen_freien_Traeger_samt_seiner_Preishistorie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int id = EnergietraegerKatalogCtrl.Neu(Name(), "Prüfgruppe");
            Assert.True(id > 0);

            string grund;
            Assert.True(EnergietraegerKatalogCtrl.Loeschen(id, out grund));
            Assert.Equal("", grund);
            Assert.Equal(0, Zaehle("SELECT COUNT(*) FROM energy_carrier WHERE id = ?",
                                   new DbParam("@id", id)));
        }

        [Fact]
        public void Loeschen_lehnt_einen_verwendeten_Traeger_benannt_ab()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string grund;
            Assert.False(EnergietraegerKatalogCtrl.Loeschen(ERDGAS_E, out grund));

            // Der BENANNTE Grund zaehlt, was den Traeger haelt — er ist die Meldung des
            // Dialogs, nicht ein allgemeines „ging nicht".
            Assert.Contains("Projektzuordnung", grund);
            Assert.Equal(1, Zaehle("SELECT COUNT(*) FROM energy_carrier WHERE id = ?",
                                   new DbParam("@id", ERDGAS_E)));
        }

        // =================================================================================
        // AusProjektEntfernen
        // =================================================================================

        [Fact]
        public void AusProjektEntfernen_loest_die_Zuordnung_und_laesst_den_Katalogeintrag_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(1, Zaehle("SELECT COUNT(*) FROM energy_project_settings " +
                                   "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                                   new DbParam("@p", PROJEKT), new DbParam("@c", OHNE_ANLAGE)));

            string grund;
            Assert.True(EnergietraegerKatalogCtrl.AusProjektEntfernen(PROJEKT, OHNE_ANLAGE, out grund));
            Assert.Equal("", grund);

            Assert.Equal(0, Zaehle("SELECT COUNT(*) FROM energy_project_settings " +
                                   "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                                   new DbParam("@p", PROJEKT), new DbParam("@c", OHNE_ANLAGE)));
            // Die Projektpreise gehen mit, der Katalogeintrag bleibt (so sagt es die
            // Rueckfrage: „Der Katalogeintrag bleibt.").
            Assert.Equal(0, Zaehle("SELECT COUNT(*) FROM energy_price " +
                                   "WHERE id_projekt = ? AND carrier_id = ?",
                                   new DbParam("@p", PROJEKT), new DbParam("@c", OHNE_ANLAGE)));
            Assert.Equal(1, Zaehle("SELECT COUNT(*) FROM energy_carrier WHERE id = ?",
                                   new DbParam("@id", OHNE_ANLAGE)));
        }

        [Fact]
        public void AusProjektEntfernen_lehnt_ab_wenn_eine_Anlage_den_Traeger_haelt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string grund;
            Assert.False(EnergietraegerKatalogCtrl.AusProjektEntfernen(PROJEKT, MIT_ANLAGE, out grund));

            Assert.Contains("Anlage", grund);
            Assert.Equal(1, Zaehle("SELECT COUNT(*) FROM energy_project_settings " +
                                   "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                                   new DbParam("@p", PROJEKT), new DbParam("@c", MIT_ANLAGE)));
        }

        /// <summary>
        /// ET-4 (08.09.2026): Auch die elektrische Welt haelt ihren Traeger, obwohl sie
        /// keine <c>ID_Carrier</c> fuehrt — 1024 fuehrt eine Waermepumpe mit Heizstab.
        /// </summary>
        [Fact]
        public void AusProjektEntfernen_lehnt_ab_wenn_die_elektrische_Welt_den_Traeger_haelt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string grund;
            Assert.False(EnergietraegerKatalogCtrl.AusProjektEntfernen(PROJEKT, STROM, out grund));

            Assert.Contains("verwendet von", grund);
            Assert.Equal(1, Zaehle("SELECT COUNT(*) FROM energy_project_settings " +
                                   "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                                   new DbParam("@p", PROJEKT), new DbParam("@c", STROM)));
        }
    }
}
