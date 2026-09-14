using System;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis zum Anwenderbefund vom 14.09.2026</b> (Auftrag #267):
    /// „Die Energiekosten sind 0 auch nach Berechnung. Kosten sind angegeben."
    ///
    /// <para><b>Die Lage im Bestand.</b> Projekt 1026 „Beispiel WP WG 1" führt eine
    /// Wärmepumpe, eine PV-Anlage und einen Stromspeicher. Keine dieser Anlagen trägt
    /// einen eigenen <c>ID_Carrier</c>, und dem Projekt ist in
    /// <c>energy_project_settings</c> nur „Erdgas E" zugeordnet — kein Stromträger.
    /// <see cref="KostenEmissionRechner"/> fand deshalb keinen Preis für den Netzbezug
    /// (19,08 MWh/a), <c>Energiekosten</c> blieb <c>null</c>, und die Seite zeigte „—"
    /// samt der irreführenden Aufforderung, die Arbeitspreise zu prüfen.</para>
    ///
    /// <para><b>Was hier festgehalten wird — drei Dinge.</b>
    /// <list type="number">
    ///   <item><description>Der RÜCKFALL: Ohne zugeordneten Stromträger bepreist die
    ///     Kostenrechnung den Netzbezug mit dem Auslieferungsträger des Katalogs —
    ///     derselbe, den die Kostenseite anzeigt und der Assistent zuordnet
    ///     (<see cref="ProjektEnergietraegerCtrl.StandardStromTraeger"/>) — und
    ///     vermerkt das.</description></item>
    ///   <item><description>Die PREISQUELLE: Ein Preis, der ausschließlich als
    ///     Preisstand in <c>energy_price</c> gepflegt ist, zählt. Vorher kannte die
    ///     Kette nur <c>energy_project_settings</c> und den Katalog, während
    ///     <c>StromPreisCtrl</c> dieselbe Datenbank über die Historie las.</description></item>
    ///   <item><description>KEIN STILLES NULL: Bleibt die Zahl aus, nennt
    ///     <c>EnergiekostenGrund</c> den Grund samt Ausweg, und
    ///     <c>WirtschaftlichkeitCtrl</c> reicht ihn als <c>Fehlgrund</c> durch.</description></item>
    /// </list></para>
    ///
    /// <para>Jeder Fall legt seine EIGENE Arbeitskopie an — die Fälle schreiben
    /// (Trägerzuordnung, Preise). <c>[Collection("Testdatenbank")]</c>, weil
    /// <see cref="DataRepository.PfadUeberschreibung"/> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class EnergiekostenGrundTests
    {
        /// <summary>„Beispiel WP WG 1" — Wärmepumpe, PV und Stromspeicher ohne
        /// <c>ID_Carrier</c>, dem Projekt ist nur „Erdgas E" zugeordnet.</summary>
        private const int PROJEKT_WP = 1026;

        /// <summary>„Wöhler – Test2" — führt „Elektrische Energie" zugeordnet und mit
        /// Projekt-Arbeitspreis; die Gegenprobe, an der sich nichts ändern darf.</summary>
        private const int PROJEKT_MIT_STROM = 1024;

        /// <summary><c>energy_carrier.id</c> von „Elektrische Energie" — der
        /// Auslieferungsträger des Katalogs (BK1).</summary>
        private const int STROM = 60;

        // =================================================================
        // 1 — Der Rückfall auf den Auslieferungs-Stromträger
        // =================================================================

        /// <summary>
        /// DER BEFUND SELBST. Ohne zugeordneten Stromträger, aber mit einem
        /// Katalogpreis für den Auslieferungsträger, entstehen Energiekosten — und der
        /// Rückfall wird benannt, statt still zu wirken.
        /// </summary>
        [Fact]
        public void Ohne_zugeordneten_Stromtraeger_bepreist_der_Auslieferungstraeger_den_Netzbezug()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.Equal(0, Emissionsquelle.StromTraeger(PROJEKT_WP));   // Ausgangslage
            Katalogpreis(STROM, 0.35);

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Null(v.EnergiekostenGrund);
            Assert.Equal("Elektrische Energie", v.StromTraegerRueckfall);
            // 19,08 MWh × 1000 × 0,35 €/kWh
            Assert.Equal(6678.0, v.Energiekosten.Value, 2);
        }

        /// <summary>
        /// Trägt auch der Auslieferungsträger keinen Preis, bleibt die Zahl aus — aber
        /// mit dem Grund, der zur Behebung führt: erst zuordnen, dann bepreisen. Der
        /// Rückfallvermerk steht dann NICHT, denn bepreist wurde nichts.
        /// </summary>
        [Fact]
        public void Ohne_Preis_nennt_der_Grund_die_fehlende_Traegerzuordnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.False(v.Energiekosten.HasValue);
            Assert.Equal(KostenEmissionRechner.GRUND_KEIN_STROMTRAEGER, v.EnergiekostenGrund);
            Assert.Null(v.StromTraegerRueckfall);
        }

        // =================================================================
        // 2 — Die Preisquelle: die Historie zählt
        // =================================================================

        /// <summary>
        /// Ein Preis, der NUR als Preisstand gepflegt ist (<c>energy_price</c>), trägt
        /// die Energiekosten. Genau diese Lage entsteht, wenn jemand in der
        /// Trägerkarte ein „Gültig ab" setzt, ohne dass die Projektspalte einen Wert
        /// bekommt — <c>custom_price_work</c> steht dann auf 0 und galt bis
        /// Auftrag #267 als „kein Preis".
        /// </summary>
        [Fact]
        public void Ein_Preis_allein_in_der_Historie_traegt_die_Energiekosten()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stromtraeger_zuordnen(PROJEKT_WP, STROM, projektpreis: 0.0);
            Historienpreis(PROJEKT_WP, STROM, 0.35, new DateTime(2026, 1, 1));

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Equal(6678.0, v.Energiekosten.Value, 2);
            Assert.Null(v.StromTraegerRueckfall);   // der Träger stand zugeordnet
        }

        /// <summary>
        /// Der PROJEKTWERT bleibt die erste Stufe: Steht er, gilt er — auch wenn die
        /// Historie einen anderen Preis führt. Sonst verschöbe diese Etappe die Zahlen
        /// jedes Projekts, das beides gepflegt hat.
        /// </summary>
        [Fact]
        public void Der_Projektwert_steht_vor_der_Historie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Stromtraeger_zuordnen(PROJEKT_WP, STROM, projektpreis: 0.40);
            Historienpreis(PROJEKT_WP, STROM, 0.35, new DateTime(2026, 1, 1));

            VariantenDaten v = Rechne(PROJEKT_WP);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Equal(7632.0, v.Energiekosten.Value, 2);   // 19,08 MWh × 0,40 €/kWh
        }

        /// <summary>
        /// Ein Projekt mit zugeordnetem Stromträger und Projektpreis rechnet
        /// unverändert — die Etappe darf keine Bestandszahl verschieben.
        /// </summary>
        [Fact]
        public void Ein_Projekt_mit_gepflegtem_Projektpreis_rechnet_unveraendert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne(PROJEKT_MIT_STROM);

            Assert.True(v.Energiekosten.HasValue);
            Assert.Equal(142696.06, v.Energiekosten.Value, 2);
            Assert.Null(v.StromTraegerRueckfall);
            Assert.Null(v.EnergiekostenGrund);
        }

        // =================================================================
        // 3 — Der Grund erreicht die Wirtschaftlichkeit
        // =================================================================

        /// <summary>
        /// <b>Nie ein stummes „—".</b> Der <c>Fehlgrund</c> des
        /// Wirtschaftlichkeitsergebnisses trägt den benannten Grund des Rechners statt
        /// des pauschalen Satzes „Arbeitspreise/Träger prüfen" — der hat den
        /// Anwenderbefund mit verursacht, weil die Preise gepflegt waren.
        /// </summary>
        [Fact]
        public void Der_Fehlgrund_der_Wirtschaftlichkeit_nennt_den_Grund_des_Rechners()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var daten = new BerichtsDaten { IdStamm = PROJEKT_WP };
            VariantenDaten stamm = Rechne(PROJEKT_WP);
            stamm.IstStamm = true;
            daten.Varianten.Add(stamm);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis erg = null;
            foreach (WirtschaftlichkeitErgebnis e in ctrl.Berechne(daten, ctrl.LadeParameter(PROJEKT_WP)))
                if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET) erg = e;

            Assert.NotNull(erg);
            Assert.Null(erg.EnergiekostenJahr);
            Assert.Equal(KostenEmissionRechner.GRUND_KEIN_STROMTRAEGER, erg.Fehlgrund);
        }

        /// <summary>
        /// Wirkt der Rückfall, steht er in der Hinweiszeile des Ergebnisses — die
        /// Zahl entsteht aus einem Träger, den das Projekt nicht führt, und das gehört
        /// gesagt.
        /// </summary>
        [Fact]
        public void Der_Rueckfall_steht_in_der_Hinweiszeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Katalogpreis(STROM, 0.35);

            var daten = new BerichtsDaten { IdStamm = PROJEKT_WP };
            VariantenDaten stamm = Rechne(PROJEKT_WP);
            stamm.IstStamm = true;
            daten.Varianten.Add(stamm);

            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitErgebnis erg = null;
            foreach (WirtschaftlichkeitErgebnis e in ctrl.Berechne(daten, ctrl.LadeParameter(PROJEKT_WP)))
                if (e.Szenario == WirtschaftlichkeitSzenario.ERWARTET) erg = e;

            Assert.NotNull(erg);
            Assert.NotNull(erg.Hinweis);
            Assert.Contains("Elektrische Energie", erg.Hinweis);
            Assert.Contains("kein Stromträger zugeordnet", erg.Hinweis);
        }

        // =================================================================
        // Handgriffe
        // =================================================================

        private static VariantenDaten Rechne(int idProjekt)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(idProjekt);
            Assert.NotNull(erg);
            var v = new VariantenDaten { IdProjekt = idProjekt, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v);
            return v;
        }

        private static void Katalogpreis(int carrierId, double preis)
        {
            DataRepository.ExecuteSQL("UPDATE energy_carrier SET price_work = ? WHERE id = ?",
                new DbParam("@p", preis), new DbParam("@c", carrierId));
        }

        private static void Stromtraeger_zuordnen(int idProjekt, int carrierId, double projektpreis)
        {
            object max = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM energy_project_settings");
            int id = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID, ID_Projekt, [ID_Energieträger], " +
                "custom_hi, custom_price_work, custom_price_base, custom_price_power) " +
                "VALUES (?, ?, ?, 1.0, ?, 0.0, 0.0)",
                new DbParam("@id", id), new DbParam("@p", idProjekt),
                new DbParam("@c", carrierId), new DbParam("@w", projektpreis));
        }

        private static void Historienpreis(int idProjekt, int carrierId, double preis, DateTime ab)
        {
            object max = DataRepository.ExecuteScalar("SELECT MAX(id) FROM energy_price");
            int id = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;
            DataRepository.ExecuteSQL(
                "INSERT INTO energy_price (id, id_projekt, carrier_id, valid_from, grundpreis, " +
                "arbeitspreis, arbeitspreis_unit, Heizwert, leistungspreis) " +
                "VALUES (?, ?, ?, ?, 0.0, ?, 'kWh', 1.0, 0.0)",
                new DbParam("@id", id), new DbParam("@p", idProjekt), new DbParam("@c", carrierId),
                new DbParam("@d", DbParamTyp.Date) { Wert = ab }, new DbParam("@a", preis));
        }
    }
}
