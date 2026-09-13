using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// AUFTRAG #247 (Anwenderentscheid SD‑E‑10 / SD‑Q15, 12.09.2026): der Knopf
    /// <b>„Ausgewählte Einheiten in Projekt übernehmen"</b> aus Schritt 1 der
    /// Stromspeicher-Auslegung.
    ///
    /// <para><b>Warum es ihn gibt.</b> Der Anwender hat entschieden, dass die
    /// Flotteneinheiten KEINE Herkunftsmarkierung tragen — <i>„die Einheiten werden nur
    /// temporär für die Optimierung benötigt"</i>. Damit aus einer gefundenen Bestückung
    /// trotzdem ein Projekt werden kann, macht dieser Weg aus den gewählten Einheiten
    /// Speicheranlagen: je Stück eine (<c>Tab_Stromspeicher</c> kennt keine Stückzahl),
    /// benannt nach der Einheit, bei mehreren Stück mit laufender Nummer.</para>
    ///
    /// <para><b>Warum ein SYNTHETISCHES Projekt.</b> Wie in
    /// <see cref="SpeicherFlottenAnlagenEinheitenTests"/>: Die Fälle legen ihren Zustand
    /// in der ARBEITSKOPIE selbst an, mit Schlüsseln weit oberhalb des Bestands. <b>Das
    /// Referenzprojekt 1046 wird dabei nicht berührt</b> — die Einfrierregel SP‑O‑8 bleibt
    /// unangetastet.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SpeicherFlottenUebernahmeTests
    {
        private const int PROJEKT = 247001;
        private const int A_SP1 = 247101;
        private const int G_SP1 = 247201;

        // =====================================================================
        //  1. Die Vorschau — reine Arithmetik, ohne einen Schreibzugriff
        // =====================================================================

        /// <summary>
        /// Eine Einheit OHNE Anlagenbezug legt n Anlagen an; eine MIT Bezug ändert ihre
        /// Anlage und legt n − 1 weitere an. Das sind die zwei Zahlen der Rückfrage.
        /// </summary>
        [Fact]
        public void Die_Vorschau_zaehlt_angelegte_und_geaenderte_Anlagen()
        {
            var frei = Einheit("e1", "Freie Einheit", null);
            var vertritt = Einheit("e2", "Speicher Halle", "4711");

            FlottenUebernahmeVorschau eine = SpeicherFlottenStudieCtrl.Vorschau(
                new[] { frei }, new[] { 3 });
            Assert.Equal(3, eine.Angelegt);
            Assert.Equal(0, eine.Geaendert);

            FlottenUebernahmeVorschau gemischt = SpeicherFlottenStudieCtrl.Vorschau(
                new[] { frei, vertritt }, new[] { 1, 2 });
            Assert.Equal(2, gemischt.Angelegt);     // 1 freie + 1 zweites Stück
            Assert.Equal(1, gemischt.Geaendert);    // die vertretene Anlage
            Assert.False(gemischt.Leer);

            Assert.True(SpeicherFlottenStudieCtrl.Vorschau(Array.Empty<FlottenEinheit>()).Leer);
        }

        /// <summary>Eine Stückzahl unter 1 gilt als 1 — wer übernimmt, will ein Gerät.</summary>
        [Fact]
        public void Eine_Stueckzahl_unter_eins_gilt_als_eins()
        {
            FlottenUebernahmeVorschau v = SpeicherFlottenStudieCtrl.Vorschau(
                new[] { Einheit("e1", "Frei", null) }, new[] { 0 });
            Assert.Equal(1, v.Angelegt);
        }

        // =====================================================================
        //  2. Die Übernahme schreibt Gerät UND Anlagenzeile
        // =====================================================================

        /// <summary>
        /// <b>Je Stück eine Anlage.</b> Zwei Stück einer freien Einheit ergeben zwei
        /// Gerätezeilen in <c>Tab_Stromspeicher</c> und zwei <c>SP_TYP</c>-Anlagen in
        /// <c>Tab_Energieanlagen</c>, deren <c>ID_SP</c> auf sie zeigt — der vollständige
        /// Anlegeweg, den auch der Projektdialog geht. Der Bezeichner trägt die laufende
        /// Nummer.
        /// </summary>
        [Fact]
        public void Zwei_Stueck_werden_zwei_Anlagen_mit_laufender_Nummer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                PROJEKT, new[] { Einheit("e1", "Growatt WIT", null) }, new[] { 2 });

            Assert.True(e.Erfolg, e.Meldung);
            Assert.Equal(2, e.Angelegt);
            Assert.Equal(0, e.Geaendert);
            Assert.Equal(new[] { "Growatt WIT 1", "Growatt WIT 2" },
                         e.Anlagen.Select(a => a.Bezeichner).ToArray());

            foreach (FlottenUebernahmeAnlage a in e.Anlagen)
            {
                Assert.True(a.AnlageId > 0);
                Assert.True(a.GeraeteId > 0);
                Assert.Equal(WizardItemClass.SP_TYP, Ganz(
                    "SELECT ID_Type FROM Tab_Energieanlagen WHERE ID = " + a.AnlageId));
                Assert.Equal(a.GeraeteId, Ganz(
                    "SELECT ID_SP FROM Tab_Energieanlagen WHERE ID = " + a.AnlageId));
                Assert.Equal(PROJEKT, Ganz(
                    "SELECT ID_Projekt FROM Tab_Stromspeicher WHERE ID = " + a.GeraeteId));
            }
        }

        /// <summary>
        /// <b>Die Werte sind die Umkehrung der Leserichtung</b>
        /// (<c>EinheitAusKatalog</c>/<c>LeseParameter</c>): Energie ← Kapazität,
        /// Leistung ← die größere Richtungsleistung, <c>Wirkungsgrad_RT</c> ←
        /// <c>eta_lade · eta_entlade</c>, Ladezustand ← Start-SoC in Prozent, dazu die drei
        /// Investitionssätze und der Hilfsverbrauch in Watt.
        /// </summary>
        [Fact]
        public void Die_Geraetewerte_sind_die_Umkehrung_der_Leserichtung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();

            var einheit = Einheit("e1", "Prüfspeicher", null);
            einheit.KapazitaetKWh = 129.0;
            einheit.LadeleistungKw = 80.0;
            einheit.EntladeleistungKw = 100.0;
            einheit.Ladewirkungsgrad = 0.95;
            einheit.Entladewirkungsgrad = 0.94;
            einheit.SocStart = 0.42;
            einheit.HilfsverbrauchKw = 0.025;
            einheit.InvestitionEuroProKWh = 320.0;
            einheit.InvestitionEuroProKw = 150.0;
            einheit.InvestitionEuro = 4500.0;

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                PROJEKT, new[] { einheit });
            Assert.True(e.Erfolg, e.Meldung);
            int geraet = Assert.Single(e.Anlagen).GeraeteId;

            Assert.Equal(129.0, Zahl("Energie", geraet), 6);
            Assert.Equal(100.0, Zahl("Leistung", geraet), 6);          // die GRÖSSERE Richtung
            Assert.Equal(0.95 * 0.94, Zahl("Wirkungsgrad_RT", geraet), 6);
            Assert.Equal(42.0, Zahl("Ladezustand", geraet), 6);
            Assert.Equal(320.0, Zahl("Modulkosten", geraet), 6);
            Assert.Equal(150.0, Zahl("Leistungskosten", geraet), 6);
            Assert.Equal(4500.0, Zahl("Investition_Fix", geraet), 6);
            Assert.Equal(25.0, Zahl("Standby_Verbrauch", geraet), 6);  // kW → W
        }

        /// <summary>
        /// <b>Rückschreiben statt Dublette.</b> Eine Einheit, die schon eine Projektanlage
        /// vertritt, ändert DIESE Anlage; ihre Gerätezeile behält Kennung und Bezeichner,
        /// und es entsteht keine zweite Anlage.
        /// </summary>
        [Fact]
        public void Eine_vertretene_Anlage_wird_geaendert_und_nicht_verdoppelt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Speicher Halle", energie: 50.0, leistung: 25.0);
            Speicheranlage(A_SP1, "Speicher Halle", G_SP1);

            var einheit = Einheit("e1", "Speicher Halle",
                                  A_SP1.ToString(CultureInfo.InvariantCulture));
            einheit.KapazitaetKWh = 129.0;
            einheit.LadeleistungKw = 100.0;
            einheit.EntladeleistungKw = 100.0;

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                PROJEKT, new[] { einheit });

            Assert.True(e.Erfolg, e.Meldung);
            Assert.Equal(0, e.Angelegt);
            Assert.Equal(1, e.Geaendert);

            FlottenUebernahmeAnlage a = Assert.Single(e.Anlagen);
            Assert.Equal(A_SP1, a.AnlageId);
            Assert.Equal(G_SP1, a.GeraeteId);
            Assert.False(a.Neu);

            Assert.Equal(129.0, Zahl("Energie", G_SP1), 6);
            Assert.Equal(100.0, Zahl("Leistung", G_SP1), 6);
            Assert.Equal("Speicher Halle", Text("Bezeichner", G_SP1));
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = " +
                                 PROJEKT + " AND ID_Type = " + WizardItemClass.SP_TYP));
        }

        /// <summary>
        /// Bei mehreren Stück bekommt das ERSTE die vertretene Anlage, jedes weitere
        /// entsteht daneben als neue Anlage (Konzept 8.3).
        /// </summary>
        [Fact]
        public void Weitere_Stueck_einer_vertretenen_Einheit_entstehen_neu()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Speicher Halle", energie: 50.0, leistung: 25.0);
            Speicheranlage(A_SP1, "Speicher Halle", G_SP1);

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                PROJEKT,
                new[] { Einheit("e1", "Speicher Halle", A_SP1.ToString(CultureInfo.InvariantCulture)) },
                new[] { 3 });

            Assert.True(e.Erfolg, e.Meldung);
            Assert.Equal(1, e.Geaendert);
            Assert.Equal(2, e.Angelegt);
            Assert.Equal(3, Ganz("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = " +
                                 PROJEKT + " AND ID_Type = " + WizardItemClass.SP_TYP));
        }

        /// <summary>
        /// <b>Der Bezeichner bleibt im Projekt eindeutig.</b> Trägt das Projekt schon ein
        /// Gerät dieses Namens, bekommt das neue den Zusatz „(2)" — dieselbe Regel wie
        /// <c>AnlagenEindeutigkeit.EindeutigerBezeichner</c>, nur IN der Transaktion
        /// geprüft.
        /// </summary>
        [Fact]
        public void Ein_belegter_Bezeichner_bekommt_einen_Zusatz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            ProjektAnlegen();
            SpeicherGeraet(G_SP1, "Growatt WIT", energie: 50.0, leistung: 25.0);

            FlottenUebernahmeErgebnis e = SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen(
                PROJEKT, new[] { Einheit("e1", "Growatt WIT", null) });

            Assert.True(e.Erfolg, e.Meldung);
            Assert.Equal("Growatt WIT (2)", Assert.Single(e.Anlagen).Bezeichner);
        }

        /// <summary>Ohne Projekt und ohne Einheit wird nichts geschrieben — und es sagt es.</summary>
        [Fact]
        public void Ohne_Projekt_oder_Einheit_wird_nichts_geschrieben()
        {
            FlottenUebernahmeErgebnis ohneProjekt = SpeicherFlottenStudieCtrl
                .EinheitenInProjektUebernehmen(0, new[] { Einheit("e1", "Frei", null) });
            Assert.False(ohneProjekt.Erfolg);
            Assert.NotEmpty(ohneProjekt.Meldung);

            FlottenUebernahmeErgebnis ohneEinheit = SpeicherFlottenStudieCtrl
                .EinheitenInProjektUebernehmen(PROJEKT, Array.Empty<FlottenEinheit>());
            Assert.False(ohneEinheit.Erfolg);
            Assert.NotEmpty(ohneEinheit.Meldung);
        }

        // =====================================================================
        //  Prüfstand
        // =====================================================================

        private static FlottenEinheit Einheit(string id, string name, string anlage) => new()
        {
            Id = id,
            Name = name,
            AnlageId = anlage,
            KapazitaetKWh = 20.0,
            LadeleistungKw = 10.0,
            EntladeleistungKw = 10.0,
            Ladewirkungsgrad = 0.95,
            Entladewirkungsgrad = 0.95,
            SocMin = 0.1,
            SocMax = 0.9,
            SocStart = 0.5
        };

        private static void ProjektAnlegen()
            => Sql("INSERT INTO Tab_Projekt (ID, Projektname) VALUES (" + PROJEKT +
                   ", '#247 Uebernahme')");

        private static void SpeicherGeraet(int id, string bezeichner, double energie, double leistung)
            => Sql("INSERT INTO Tab_Stromspeicher (ID, ID_Projekt, Bezeichner, Leistung, Energie, Wirkungsgrad_RT) " +
                   "VALUES (" + id + ", " + PROJEKT + ", '" + bezeichner + "', " +
                   Z(leistung) + ", " + Z(energie) + ", " + Z(0.9) + ")");

        /// <summary>Die sieben Geräte-Verweisspalten — ihr Vorgabewert 0 wäre ein
        /// Fremdschlüssel auf ein Gerät, das es nicht gibt (Muster
        /// <see cref="SpeicherFlottenAnlagenEinheitenTests"/>).</summary>
        private static readonly string[] GERAETEVERWEISE =
        { "ID_WP", "ID_Kessel", "ID_BHKW", "ID_PV", "ID_Solar", "ID_SP", "ID_PUFFER" };

        private static void Speicheranlage(int idAnlage, string bezeichner, int geraet)
        {
            string spalten = "ID, ID_Projekt, Bezeichner, ID_Type";
            string werte = idAnlage + ", " + PROJEKT + ", '" + bezeichner + "', " +
                           WizardItemClass.SP_TYP;
            foreach (string s in GERAETEVERWEISE)
            {
                spalten += ", [" + s + "]";
                werte += ", " + (s == "ID_SP" ? geraet.ToString(CultureInfo.InvariantCulture) : "NULL");
            }
            Sql("INSERT INTO Tab_Energieanlagen (" + spalten + ") VALUES (" + werte + ")");
        }

        private static double Zahl(string spalte, int geraeteId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [" + spalte + "] FROM Tab_Stromspeicher WHERE ID = ?",
                new DbParam("@id", geraeteId));
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string spalte, int geraeteId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [" + spalte + "] FROM Tab_Stromspeicher WHERE ID = ?",
                new DbParam("@id", geraeteId));
            return o == null || o == DBNull.Value ? "" : o.ToString();
        }

        private static int Ganz(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static string Z(double wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static void Sql(string sql) { DataRepository.ExecuteSQL(sql); }
    }
}
