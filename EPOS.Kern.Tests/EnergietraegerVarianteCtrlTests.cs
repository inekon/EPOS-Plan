using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schreibweg der Energietraegervariante (iU9-W6.0a/b) auf einer Arbeitskopie der
    /// Testdatenbank.
    ///
    /// <para>Soll sind die vier Ausgaenge von
    /// <c>Form_Heizkessel.CreateNewEnergyCarrier</c> bzw.
    /// <c>Form_BHKWEing.CreateNewEnergyCarrier</c>, die bis Welle 6 nur als
    /// <c>MessageBox</c>-Text unterscheidbar waren. Der Ausgang entscheidet, ob der
    /// Aufrufer den Erzeuger aufnimmt (jeder Fall ausser <c>Fehler</c>) - deshalb sind es
    /// vier Faelle und nicht ein „hat geklappt".</para>
    /// </summary>
    public class EnergietraegerVarianteCtrlTests
    {
        /// <summary>Erdgas E - Kategorie 1 (Gas), im Bestand der Testdatenbank.</summary>
        private const int ERDGAS_E = 3;

        /// <summary>Projekt 1017 des Regressionsnetzes; es fuehrt bereits Traegerzuordnungen.</summary>
        private const int PROJEKT = 1017;

        /// <summary>Traeger 54 („Strom Variante") ist Projekt 1017 zugeordnet.</summary>
        private const int TRAEGER_1017 = 54;

        private static string Name() => "Pruefvariante " + Guid.NewGuid().ToString("N").Substring(0, 8);

        // =================================================================================
        // Anlegen - die vier Ausgaenge
        // =================================================================================

        [Fact]
        public void Ohne_Projekt_wird_nur_vorgemerkt()
        {
            // 1b) im Bestand: "Wizard / kein echtes Projekt: nur der Katalog-Traeger."
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var e = EnergietraegerVarianteCtrl.Anlegen(0, false, ERDGAS_E, "Erdgas E", Name());

            Assert.Equal(EnergietraegerVarianteCtrl.VariantenAnlage.Vorgemerkt, e.Ausgang);
            Assert.True(e.CarrierId > 0);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
        }

        [Fact]
        public void Im_Assistenten_wird_auch_mit_Projekt_nur_vorgemerkt()
        {
            // energy_price und energy_Project_settings haengen an Tab_Projekt.ID, die es
            // im Assistenten noch nicht gibt - dieselbe Weiche wie im Bestand.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var e = EnergietraegerVarianteCtrl.Anlegen(PROJEKT, true, ERDGAS_E, "Erdgas E", Name());

            Assert.Equal(EnergietraegerVarianteCtrl.VariantenAnlage.Vorgemerkt, e.Ausgang);
            Assert.True(e.CarrierId > 0);
        }

        [Fact]
        public void Mit_Projekt_wird_angelegt_und_zugeordnet()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = Name();
            var e = EnergietraegerVarianteCtrl.Anlegen(PROJEKT, false, ERDGAS_E, "Erdgas E", name);

            Assert.Equal(EnergietraegerVarianteCtrl.VariantenAnlage.Angelegt, e.Ausgang);
            Assert.True(e.CarrierId > 0);

            // Beide projektbezogenen Saetze stehen - die Transaktion hat committet.
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                new DbParam("@p", PROJEKT), new DbParam("@e", e.CarrierId))));
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_price WHERE id_projekt = ? AND carrier_id = ?",
                new DbParam("@p", PROJEKT), new DbParam("@e", e.CarrierId))));
        }

        [Fact]
        public void Ein_zweiter_Aufruf_meldet_bereits_zugeordnet()
        {
            // Der Traeger wird ueber seinen Namen wiedergefunden (SELECT id FROM
            // energy_carrier WHERE name = ?) und NICHT gedoppelt.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string name = Name();
            var erst = EnergietraegerVarianteCtrl.Anlegen(PROJEKT, false, ERDGAS_E, "Erdgas E", name);
            var zweit = EnergietraegerVarianteCtrl.Anlegen(PROJEKT, false, ERDGAS_E, "Erdgas E", name);

            Assert.Equal(EnergietraegerVarianteCtrl.VariantenAnlage.BereitsZugeordnet, zweit.Ausgang);
            Assert.Equal(erst.CarrierId, zweit.CarrierId);
            Assert.Contains(name, zweit.Meldung);

            // Kein zweiter Katalogsatz gleichen Namens.
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_carrier WHERE name = ?", new DbParam("@n", name))));
        }

        [Fact]
        public void Ein_zu_langer_Name_faellt_in_den_Fehlerzweig()
        {
            // energy_carrier.name traegt CHECK (length(name) <= 64). Der Vorlaeufer lief
            // hier in seinen catch-Zweig ("Fehler beim Speichern: …") und meldete
            // carrierId 0 - genau das Signal, an dem btn_Kessel_Hinzu_Click abbrach.
            // Es ist der einzige von aussen ausloesbare Fehlerfall des Schreibwegs.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var e = EnergietraegerVarianteCtrl.Anlegen(PROJEKT, false, ERDGAS_E, "Erdgas E",
                                                       new string('x', 200));

            Assert.Equal(EnergietraegerVarianteCtrl.VariantenAnlage.Fehler, e.Ausgang);
            Assert.Equal(0, e.CarrierId);
            Assert.False(string.IsNullOrEmpty(e.Meldung));
        }

        [Fact]
        public void Ein_unbekannter_Brennstoff_legt_den_Traeger_ohne_Gruppe_an()
        {
            // BEFUND W6 (Bestandsverhalten, nicht geaendert): Ergaenzen findet keine
            // Kategorie, group_code/pricing_model/billing_unit bleiben NULL - und
            // energy_carrier laesst das zu (keine NOT-NULL-Bedingung auf diesen Spalten).
            // Der Traeger entsteht also, nur ohne Gruppe. Der Aufrufer nimmt den Erzeuger
            // damit auf. Weil die SQL zeichengleich uebernommen ist, gilt das seit je;
            // die Probe haelt es fest, statt es stillschweigend zu reparieren (Regel F3).
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var e = EnergietraegerVarianteCtrl.Anlegen(0, false, 999999, "gibt es nicht", Name());

            Assert.Equal(EnergietraegerVarianteCtrl.VariantenAnlage.Vorgemerkt, e.Ausgang);
            Assert.True(e.CarrierId > 0);
            Assert.True(DataRepository.ExecuteScalar(
                "SELECT group_code FROM energy_carrier WHERE id = ?",
                new DbParam("@id", e.CarrierId)) is null or DBNull);
        }

        // =================================================================================
        // VariantenDerGruppe
        // =================================================================================

        [Fact]
        public void Die_Gruppe_liefert_ihre_Varianten_nach_Namen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var (gruppe, varianten) = EnergietraegerVarianteCtrl.VariantenDerGruppe(TRAEGER_1017);

            Assert.False(string.IsNullOrEmpty(gruppe));
            Assert.NotEmpty(varianten);
            Assert.Contains(varianten, v => v.Id == TRAEGER_1017);

            // ORDER BY name - die Liste kommt sortiert an.
            var namen = new List<string>();
            foreach (var v in varianten) namen.Add(v.Name);
            var sortiert = new List<string>(namen);
            sortiert.Sort(StringComparer.Ordinal);
            Assert.Equal(sortiert, namen);
        }

        [Fact]
        public void Ein_unbekannter_Traeger_liefert_keine_Gruppe()
        {
            // Im Bestand setzte die Maske hier DataSource = null.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var (gruppe, varianten) = EnergietraegerVarianteCtrl.VariantenDerGruppe(999999);

            Assert.Null(gruppe);
            Assert.Empty(varianten);
        }

        // =================================================================================
        // TraegerUmhaengen
        // =================================================================================

        [Fact]
        public void Umhaengen_verschiebt_genau_die_Projektzeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            // Ein frischer Traeger als Ziel, damit kein Bestandssatz verschoben wird.
            var neu = EnergietraegerVarianteCtrl.Anlegen(0, false, ERDGAS_E, "Erdgas E", Name());
            Assert.True(neu.CarrierId > 0);

            Assert.True(EnergietraegerVarianteCtrl.TraegerUmhaengen(PROJEKT, TRAEGER_1017, neu.CarrierId));

            Assert.Equal(0, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                new DbParam("@p", PROJEKT), new DbParam("@e", TRAEGER_1017))));
            Assert.Equal(1, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                new DbParam("@p", PROJEKT), new DbParam("@e", neu.CarrierId))));
        }

        [Fact]
        public void Umhaengen_eines_nicht_zugeordneten_Traegers_ist_kein_Fehler()
        {
            // Bestandsverhalten: Das UPDATE trifft keine Zeile. Der Wechsel gilt dann nur
            // im Modell und wird beim Speichern des Projekts wirksam.
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(EnergietraegerVarianteCtrl.TraegerUmhaengen(PROJEKT, 999998, 999999));
        }
    }
}
