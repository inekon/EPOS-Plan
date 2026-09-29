using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein älteres Projektpaket wird beim Import angehoben</b> (Konzept
    /// <c>Dokumentation/ueberholt/Konzept_Projektpaket_Migration_EPOS-Plan.md</c>, Prüfweg).
    ///
    /// <para>Alte Pakete liegen nicht im Repositorium. Die Fälle exportieren deshalb aus der
    /// Testdatenbank und bauen das Paket auf den Stand 93 zurück: Schemastand im Manifest,
    /// Spaltenbild und Werte, wie sie vor den umrechnenden Schritten standen. Nach dem
    /// Import müssen die Werte der Quelle wieder dastehen.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProjektpaketAnhebungTests
    {
        /// <summary>Projekt 1049: BHKW, Gebäude und Kollektorfeld.</summary>
        private const string SOLAR = "Referenzprojekt Solarthermie";

        /// <summary>Projekt 1017: Wirtschaftlichkeit, drei Trägerzeilen.</summary>
        private const string WP = "WP_PV-Speicher";

        private const int ALTSTAND = 93;

        // =============================================================================
        //  Das Register
        // =============================================================================

        [Fact]
        public void Jeder_Schemaschritt_bis_zum_Zielstand_nennt_seine_Paketwirkung()
        {
            var nummern = Paketanhebung.Stufen.Select(s => s.Nr).ToList();
            var erwartet = Enumerable.Range(Paketanhebung.UNTERE_GRENZE + 1,
                                            SchemaStand.Zielversion - Paketanhebung.UNTERE_GRENZE).ToList();
            Assert.Equal(erwartet, nummern);

            foreach (Paketanhebung.Stufe s in Paketanhebung.Stufen)
            {
                Assert.False(string.IsNullOrWhiteSpace(s.Text), "Schritt " + s.Nr + " ohne Text.");
                Assert.True(s.Wirkung == Paketanhebung.Art.Umformung
                            ? s.Umformung != null : s.Umformung == null,
                            "Schritt " + s.Nr + ": Art und Umformung passen nicht zusammen.");
            }
        }

        [Fact]
        public void Die_Vorschau_zaehlt_Schritte_und_Umformungen()
        {
            Paketanhebung.Vorschau v = Paketanhebung.Vorschauen(ALTSTAND);
            Assert.True(v.Noetig);
            Assert.False(v.Neuer);
            Assert.False(v.UnterGrenze);
            Assert.Equal(SchemaStand.Zielversion - ALTSTAND, v.Schritte);
            Assert.Equal(Paketanhebung.Stufen.Count(s => s.Nr > ALTSTAND && s.Wirkung == Paketanhebung.Art.Umformung),
                         v.Umformungen);

            Assert.False(Paketanhebung.Vorschauen(SchemaStand.Zielversion).Noetig);
            Assert.False(Paketanhebung.Vorschauen(0).Noetig);
            Assert.True(Paketanhebung.Vorschauen(SchemaStand.Zielversion + 1).Neuer);
            Assert.True(Paketanhebung.Vorschauen(Paketanhebung.UNTERE_GRENZE - 1).UnterGrenze);
            Assert.False(Paketanhebung.Vorschauen(Paketanhebung.UNTERE_GRENZE).UnterGrenze);
            Assert.Equal(61, Paketanhebung.UNTERE_GRENZE);
        }

        // =============================================================================
        //  Rückgebautes Paket: BHKW, Gebäude, Kollektor (Schritte 98, 99, 101, 148, 150)
        // =============================================================================

        [Fact]
        public void Ein_Paket_auf_Stand_93_wird_gehoben_und_traegt_die_Werte_der_Quelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(SOLAR);
            Assert.True(quelle > 0);
            DataTable bhkw = DataRepository.GetDataTable(
                "SELECT Bezeichner, Wirkungsgrad, Wirkungsgrad_el, Wirkungsgrad_th FROM Tab_BHKW WHERE ID_Projekt = ? ORDER BY Bezeichner",
                new DbParam("@p", quelle));
            DataTable gebaeude = DataRepository.GetDataTable(
                "SELECT Gebaeudename, Nutzflaeche FROM Tab_Gebaeude WHERE ID_Projekt = ? ORDER BY Gebaeudename",
                new DbParam("@p", quelle));
            Assert.True(bhkw.Rows.Count > 0 && gebaeude.Rows.Count > 0);

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(SOLAR, paket));

            string alt = ordner.Datei("alt.wpx");
            Umbauen(paket, alt, ALTSTAND, (tabelle, zeile) =>
            {
                if (tabelle == "Tab_BHKW")
                {
                    // Vor 98 stand dort der ELEKTRISCHE Wirkungsgrad in Prozent.
                    double w = Zahl(zeile["Wirkungsgrad"]), pel = Zahl(zeile["Pel"]), pth = Zahl(zeile["Ptherm"]);
                    zeile["Wirkungsgrad"] = Math.Round(w * pel / (pel + pth) * 100.0, 6);
                    zeile.Remove("Wirkungsgrad_el");
                    zeile.Remove("Wirkungsgrad_th");
                }
                else if (tabelle == "Tab_Gebaeude")
                {
                    zeile["Wohnflaeche"] = zeile["Nutzflaeche"]?.DeepClone();
                    zeile.Remove("Nutzflaeche");
                    zeile.Remove("Baujahr");
                    zeile.Remove("Energiestandard");
                    zeile["Baualtersklasse"] = "I";       // alte Klasse I: Niedrigenergiebauweise
                }
                else if (tabelle == "Tab_Solarkollektoren")
                {
                    zeile["Vorlauf"] = 60;                 // vor 150 führte der Kollektor sie
                    zeile["Ruecklauf"] = 30;
                }
            });

            int neu = io.Importieren(alt, "Anhebung 93", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            Assert.Contains(io.LetzterBericht, z => z.Contains(ALTSTAND.ToString(CultureInfo.InvariantCulture))
                                                    && z.Contains(SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture)));
            Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt 98:", StringComparison.Ordinal));
            Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt 148:", StringComparison.Ordinal));

            DataTable bhkwNeu = DataRepository.GetDataTable(
                "SELECT Bezeichner, Wirkungsgrad, Wirkungsgrad_el, Wirkungsgrad_th FROM Tab_BHKW WHERE ID_Projekt = ? ORDER BY Bezeichner",
                new DbParam("@p", neu));
            Assert.Equal(bhkw.Rows.Count, bhkwNeu.Rows.Count);
            for (int i = 0; i < bhkw.Rows.Count; i++)
                foreach (string s in new[] { "Wirkungsgrad", "Wirkungsgrad_el", "Wirkungsgrad_th" })
                    Assert.True(Math.Abs(Zahl(bhkw.Rows[i][s]) - Zahl(bhkwNeu.Rows[i][s])) < 1e-3,
                                "Tab_BHKW." + s + ": Quelle " + bhkw.Rows[i][s] + ", nach der Anhebung " + bhkwNeu.Rows[i][s]);

            DataTable gebNeu = DataRepository.GetDataTable(
                "SELECT Gebaeudename, Nutzflaeche, Baualtersklasse, Energiestandard FROM Tab_Gebaeude WHERE ID_Projekt = ? ORDER BY Gebaeudename",
                new DbParam("@p", neu));
            Assert.Equal(gebaeude.Rows.Count, gebNeu.Rows.Count);
            for (int i = 0; i < gebaeude.Rows.Count; i++)
            {
                Assert.Equal(Zahl(gebaeude.Rows[i]["Nutzflaeche"]), Zahl(gebNeu.Rows[i]["Nutzflaeche"]), 6);
                Assert.Equal("J", Convert.ToString(gebNeu.Rows[i]["Baualtersklasse"], CultureInfo.InvariantCulture));
                Assert.Equal(Energiestandard.NIEDRIGENERGIE, Convert.ToString(gebNeu.Rows[i]["Energiestandard"], CultureInfo.InvariantCulture));
            }

            object kollektor = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Solarkollektoren WHERE ID_Projekt = ?", new DbParam("@p", neu));
            Assert.Equal(1L, Convert.ToInt64(kollektor, CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Rückgebautes Paket: Wirtschaftlichkeit und Trägerkarte (Schritte 102, 112, 127)
        // =============================================================================

        [Fact]
        public void Freitext_Preisbasis_und_Anlagenart_werden_beim_Heben_nachgezogen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(WP);
            Assert.True(quelle > 0);
            const string FREITEXT = "Imagegewinn beim Betreiber";
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = ? WHERE ID_Projekt = ?",
                new DbParam("@t", FREITEXT), new DbParam("@p", quelle)));
            List<string> basis = Preisbasen(quelle);
            Assert.NotEmpty(basis);

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(WP, paket));

            string alt = ordner.Datei("alt.wpx");
            Umbauen(paket, alt, ALTSTAND, (tabelle, zeile) =>
            {
                if (tabelle == "energy_project_settings") zeile.Remove("Preisbasis");
                else if (tabelle == "Tab_Energieanlagen") zeile["KWKG_Anlagenart"] = "";
            });

            int neu = io.Importieren(alt, "Anhebung 93b", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            Assert.Equal(basis, Preisbasen(neu));

            object leer = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND KWKG_Anlagenart = ''",
                new DbParam("@p", neu));
            Assert.Equal(0L, Convert.ToInt64(leer, CultureInfo.InvariantCulture));

            DataTable wirkung = DataRepository.GetDataTable(
                "SELECT Kategorie, Beschreibung FROM Tab_ProjektWirkung WHERE ID_Projekt = ?", new DbParam("@p", neu));
            Assert.Equal(1, wirkung.Rows.Count);
            Assert.Equal(ProjektWirkungSchema.KATEGORIE_UEBERNAHME, Convert.ToString(wirkung.Rows[0]["Kategorie"], CultureInfo.InvariantCulture));
            Assert.Equal(FREITEXT, Convert.ToString(wirkung.Rows[0]["Beschreibung"], CultureInfo.InvariantCulture));
        }

        // =============================================================================
        //  Zielstand unverändert, Fehler lässt die Datenbank unberührt
        // =============================================================================

        [Fact]
        public void Ein_Paket_auf_Zielstand_wird_nicht_angefasst()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("ziel.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(SOLAR, paket));

            int neu = io.Importieren(paket, "Zielstand", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.DoesNotContain(io.LetzterBericht, z => z.StartsWith("Schritt ", StringComparison.Ordinal));
        }

        [Fact]
        public void Scheitert_eine_Stufe_bricht_der_Import_ab_und_die_Datenbank_bleibt_unberuehrt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(WP);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET Nicht_Monetaer = ? WHERE ID_Projekt = ?",
                new DbParam("@t", "Freitext"), new DbParam("@p", quelle)));

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(WP, paket));

            // Eine Projekt-Id als Text: Die STRICT-Tabelle der Wirkungen (Schritt 127)
            // nimmt sie nicht an — die Stufe scheitert.
            string kaputt = ordner.Datei("kaputt.wpx");
            Umbauen(paket, kaputt, ALTSTAND, (tabelle, zeile) =>
            {
                if (tabelle == "Tab_Projekt") zeile["ID"] = "x" + zeile["ID"];
                else if (tabelle == "Tab_ProjektWirtschaftlichkeit") zeile["ID_Projekt"] = "x" + zeile["ID_Projekt"];
            });

            long vorher = Anzahl("Tab_Projekt");
            int neu = io.Importieren(kaputt, "Anhebung kaputt", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.Equal(-1, neu);
            Assert.Contains("127", fehler, StringComparison.Ordinal);
            Assert.Equal(vorher, Anzahl("Tab_Projekt"));
        }

        // =============================================================================
        //  Stufe 2: Pakete auf Stand 61 — alle Stufen 62 bis zum Zielstand laufen
        // =============================================================================

        /// <summary>Der Stand unter dem ersten Registerschritt: jede Stufe läuft.</summary>
        private const int URSTAND = 61;

        /// <summary>Projekt mit zwei PV-Modulen in der Projektkopie.</summary>
        private const string PV = "Laurentiuskirche";

        [Fact]
        public void Gruppe_Anlagen_Leistungsgrenze_Heizstab_Speichervariante_und_KWK_Zuschlag_werden_nachgezogen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(WP);
            Assert.True(quelle > 0);
            long varianten = Wert("SELECT COUNT(*) FROM Tab_StromspeicherVariante v INNER JOIN Tab_Energieanlagen a ON a.ID = v.ID_Energieanlage WHERE a.ID_Projekt = ?", quelle);
            Assert.True(varianten > 0);

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(WP, paket));

            string alt = ordner.Datei("alt.wpx");
            UmbauenMit(paket, alt, URSTAND, (pfad, zeilen) =>
            {
                if (Ist(pfad, "Tab_Einstellungen"))
                    foreach (JsonObject z in zeilen.Select(n => n.AsObject()))
                    {
                        z["Leistungsgrenze"] = null;                 // vor 67 ohne Wert
                        z[HeizstabJeWaermepumpe.SPALTE_PROJEKT] = 1;  // vor 79 am Projekt
                    }
                else if (Ist(pfad, "Tab_Energieanlagen"))
                    foreach (JsonObject z in zeilen.Select(n => n.AsObject()))
                    {
                        long typ = (long)Zahl(z["ID_Type"]);
                        if (typ == WizardItemClass.WP_TYP) z[HeizstabJeWaermepumpe.SPALTE_ANLAGE] = 0;
                        if (typ == WizardItemClass.BHKW_TYP) z[SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN] = null;
                    }
                else if (Ist(pfad, "Tab_ProjektWirtschaftlichkeit"))
                    foreach (JsonObject z in zeilen.Select(n => n.AsObject()))
                        z[KwkgProjektaltspalten.SPALTE_BONUS] = 7.5;   // vor 89 am Projekt
                else if (Ist(pfad, "Tab_StromspeicherVariante"))
                {
                    // Vor 87 durfte ein Projekt zwei aktive Varianten führen.
                    JsonObject erste = zeilen[0].AsObject();
                    erste["Aktiv"] = 1;
                    JsonObject zweite = erste.DeepClone().AsObject();
                    zweite["ID"] = (long)Zahl(erste["ID"]) + 100000;
                    zeilen.Add(zweite);
                }
            });

            int neu = io.Importieren(alt, "Anhebung 61a", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            foreach (int s in new[] { 67, 79, 87, 89 })
                Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt " + s + ":", StringComparison.Ordinal));

            Assert.Equal(0L, Wert("SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = ? AND IFNULL(Leistungsgrenze, -1) <> " +
                                  BhkwLeistungsgrenzeVorgabe.VORGABE_PROZENT, neu));
            Assert.Equal(0L, Wert("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = " + WizardItemClass.WP_TYP +
                                  " AND IFNULL(Heizstab, 0) <> 1", neu));
            Assert.True(Wert("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = " + WizardItemClass.WP_TYP, neu) > 0);
            Assert.Equal(0L, Wert("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = " + WizardItemClass.BHKW_TYP +
                                  " AND IFNULL(KWKG_Satz_Eigen, 0) <> 7.5", neu));
            Assert.Equal(varianten + 1, Wert("SELECT COUNT(*) FROM Tab_StromspeicherVariante v INNER JOIN Tab_Energieanlagen a ON a.ID = v.ID_Energieanlage WHERE a.ID_Projekt = ?", neu));
            Assert.Equal(1L, Wert("SELECT COUNT(*) FROM Tab_StromspeicherVariante v INNER JOIN Tab_Energieanlagen a ON a.ID = v.ID_Energieanlage WHERE a.ID_Projekt = ? AND v.Aktiv = 1", neu));
        }

        [Fact]
        public void Gruppe_Strompreis_Traegersatz_Faltung_und_Verguetungsumzug_werden_nachgezogen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(WP);
            Assert.True(quelle > 0);
            List<long> strom = DataRepository.GetDataTable(
                "SELECT e.ID FROM energy_project_settings e INNER JOIN energy_carrier c ON c.id = e.[ID_Energieträger] " +
                "WHERE e.ID_Projekt = ? AND c.pricing_model = ? ORDER BY e.ID",
                new DbParam("@p", quelle), new DbParam("@m", StrompreisZerlegung.PREISMODELL_STROM))
                .Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r[0], CultureInfo.InvariantCulture)).ToList();
            Assert.NotEmpty(strom);
            long traeger = Wert("SELECT [ID_Energieträger] FROM energy_project_settings WHERE ID = ?", strom[0]);
            long saetze = Wert("SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ?", quelle);
            List<double> preiseVorher = Preise(quelle, traeger);
            double eigenVorher = Zahl(DataRepository.ExecuteScalar(
                "SELECT custom_price_work FROM energy_project_settings WHERE ID = ?", new DbParam("@i", strom[0])));

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(WP, paket));

            string[] ab83 =
            {
                SchemaKatalog.SPALTE_AUFSCHLAG_BESCHAFFUNG, SchemaKatalog.SPALTE_AUFSCHLAG_KWKG,
                SchemaKatalog.SPALTE_AUFSCHLAG_OFFSHORE, "Aufschlag_StromNEV19",
            };
            string alt = ordner.Datei("alt.wpx");
            UmbauenMit(paket, alt, URSTAND, (pfad, zeilen) =>
            {
                if (Ist(pfad, "energy_project_settings"))
                {
                    foreach (JsonObject z in zeilen.Select(n => n.AsObject()))
                    {
                        // Vor 83: kein Beschaffungsanteil, ein Aufschlagsmodus; vor 84: Vergütung an der Karte.
                        foreach (string s in ab83)
                        {
                            z.Remove(s);
                            z.Remove(s + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX);
                        }
                        z.Remove(SchemaKatalog.SPALTE_AUFSCHLAG_UMLAGEN_EINZELN);
                        z[StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS] = "";
                        bool istStrom = strom.Contains((long)Zahl(z["ID"]));
                        z[StrompreisAltspalten.SPALTE_VERGUETUNG_PV] = istStrom ? 8.2 : 0.0;
                        z[StrompreisAltspalten.SPALTE_VERGUETUNG_BHKW] = 0.0;
                        if ((long)Zahl(z["ID"]) == strom[0])
                        {
                            z[StrompreisAltspalten.SPALTE_AUFSCHLAG_MODUS] = StrompreisZerlegung.MODUS_AUFGESCHLUESSELT;
                            foreach (string s in StrompreisZerlegung.BESTANDSANTEILE)
                                z[s + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX] = 0;
                            z[SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT] = 2.0;
                            z[SchemaKatalog.SPALTE_AUFSCHLAG_NETZENTGELT + SchemaKatalog.SPALTE_AUFSCHLAG_AKTIV_SUFFIX] = 1;
                        }
                    }
                    // Vor 76 durfte ein Träger zweimal am Projekt stehen.
                    JsonObject doppel = zeilen.Select(n => n.AsObject()).First(z => (long)Zahl(z["ID"]) == strom[0]).DeepClone().AsObject();
                    doppel["ID"] = strom[0] + 100000;
                    doppel["custom_price_work"] = 99.0;
                    zeilen.Add(doppel);
                }
                else if (Ist(pfad, "Tab_ProjektWirtschaftlichkeit"))
                    foreach (JsonObject z in zeilen.Select(n => n.AsObject()))
                        z["Einspeiseverguetung"] = null;
            });

            int neu = io.Importieren(alt, "Anhebung 61b", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            foreach (int s in new[] { 76, 83, 84 })
                Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt " + s + ":", StringComparison.Ordinal));

            // 76: die Dublette ist fort, der ältere Satz bleibt.
            Assert.Equal(saetze, Wert("SELECT COUNT(*) FROM energy_project_settings WHERE ID_Projekt = ?", neu));

            // 83: 2 ct/kWh Aufschlag im Arbeitspreis, der alte Preis als Beschaffung.
            DataTable karte = DataRepository.GetDataTable(
                "SELECT custom_price_work, Aufschlag_Beschaffung, Aufschlag_Beschaffung_Aktiv, Aufschlag_Netzentgelt_Aktiv " +
                "FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", neu), new DbParam("@t", traeger));
            Assert.Equal(1, karte.Rows.Count);
            Assert.Equal(1L, Convert.ToInt64(karte.Rows[0]["Aufschlag_Beschaffung_Aktiv"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(karte.Rows[0]["Aufschlag_Netzentgelt_Aktiv"], CultureInfo.InvariantCulture));
            Assert.NotEqual(DBNull.Value, karte.Rows[0]["Aufschlag_Beschaffung"]);
            if (eigenVorher > 0)
                Assert.Equal(eigenVorher + 0.02, Zahl(karte.Rows[0]["custom_price_work"]), 9);
            List<double> preiseNachher = Preise(neu, traeger);
            Assert.Equal(preiseVorher.Count, preiseNachher.Count);
            for (int i = 0; i < preiseVorher.Count; i++)
                Assert.Equal(preiseVorher[i] > 0 ? preiseVorher[i] + 0.02 : preiseVorher[i], preiseNachher[i], 9);

            // 84: 8,2 ct/kWh der Karte werden 0,082 EUR/kWh der Parameter.
            Assert.Equal(0.082, Zahl(DataRepository.ExecuteScalar(
                "SELECT Einspeiseverguetung FROM Tab_ProjektWirtschaftlichkeit WHERE ID_Projekt = ?", new DbParam("@p", neu))), 9);
        }

        [Fact]
        public void Gruppe_Kosten_Nullzeilen_der_Erfassungsgruppen_fallen_weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(SOLAR);
            Assert.True(quelle > 0);
            long zeilen0 = Wert("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", quelle);
            long zentrale = Wert("SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?", DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE);
            long haupt = Wert("SELECT w.ID FROM Tab_ProjektWerte w INNER JOIN Tab_Kostenfaktor f ON f.StammID = w.StammID " +
                              "WHERE w.ProjektID = ? AND f.IsMainComponent = 1 ORDER BY w.ID LIMIT 1", quelle);
            Assert.True(zentrale > 0 && haupt > 0);

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(SOLAR, paket));

            string alt = ordner.Datei("alt.wpx");
            UmbauenMit(paket, alt, URSTAND, (pfad, zeilen) =>
            {
                if (Ist(pfad, "Tab_ProjektWerte"))
                {
                    // Vor 90 stand die Erfassungsgruppe mit einer Nullzeile im Projekt.
                    JsonObject z = zeilen.Select(n => n.AsObject()).First(x => (long)Zahl(x["ID"]) == haupt).DeepClone().AsObject();
                    z["ID"] = haupt + 100000;
                    z["KomponentenID"] = zentrale;
                    z["KategorieID"] = 1;
                    foreach (string s in new[] { "EingegebenerWert", "Worstcase", "Bestcase", "Menge", "Einheitpreis" })
                        z[s] = 0.0;
                    zeilen.Add(z);
                }
                else if (pfad.StartsWith("catalogs/", StringComparison.Ordinal) && Ist(pfad, SchemaKatalog.TAB_KOSTENKOMPONENTE) &&
                         !zeilen.Any(n => (long)Zahl(n["ID"]) == zentrale))
                    zeilen.Add(new JsonObject { ["ID"] = zentrale, [SchemaKatalog.SPALTE_KK_KOMPONENTE] = DbWerte.KOSTEN_KOMPONENTE_WAERMEZENTRALE });
            });

            int neu = io.Importieren(alt, "Anhebung 61c", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt 90:", StringComparison.Ordinal));
            Assert.Equal(zeilen0, Wert("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ?", neu));
            Assert.Equal(0L, Wert("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ? AND KomponentenID = " + zentrale, neu));
        }

        [Fact]
        public void Gruppe_PV_verdorbene_Modulkoeffizienten_werden_repariert_oder_geleert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = Id(PV);
            Assert.True(quelle > 0);
            PvModulKoeffizienten bekannt = PvKoeffizientenReparatur.AUSLIEFERUNG[0];
            const string UNBEKANNT = "Modul ohne Katalogtreffer 4711";

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PV, paket));

            string alt = ordner.Datei("alt.wpx");
            UmbauenMit(paket, alt, URSTAND, (pfad, zeilen) =>
            {
                if (!Ist(pfad, "Tab_PV") || pfad.StartsWith("catalogs/", StringComparison.Ordinal)) return;
                Assert.True(zeilen.Count >= 2);
                // Vor 69: alpha_SC als Kopie des Kurzschlussstroms, T_NOCT außerhalb des Fensters.
                JsonObject a = zeilen[0].AsObject(), b = zeilen[1].AsObject();
                a["Bezeichner"] = bekannt.Bezeichner;
                a["Firma"] = null;
                a["I_Kurzschluss"] = 9.5;
                a["alpha_SC"] = 9.5;
                b["Bezeichner"] = UNBEKANNT;
                b["T_NOCT"] = 99.0;
            });

            int neu = io.Importieren(alt, "Anhebung 61d", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            Assert.Contains(io.LetzterBericht, z => z.StartsWith("Schritt 69:", StringComparison.Ordinal));

            Assert.Equal(bekannt.AlphaSc, Zahl(DataRepository.ExecuteScalar(
                "SELECT alpha_SC FROM Tab_PV WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", neu), new DbParam("@b", bekannt.Bezeichner))), 9);
            DataTable leer = DataRepository.GetDataTable(
                "SELECT T_NOCT FROM Tab_PV WHERE ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@p", neu), new DbParam("@b", UNBEKANNT));
            Assert.Equal(1, leer.Rows.Count);
            Assert.Equal(DBNull.Value, leer.Rows[0][0]);
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        private static bool Ist(string pfad, string tabelle) =>
            string.Equals(Path.GetFileNameWithoutExtension(pfad), tabelle, StringComparison.OrdinalIgnoreCase);

        private static long Wert(string sql, object parameter)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@p", parameter));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static List<double> Preise(int projekt, long traeger) =>
            DataRepository.GetDataTable(
                "SELECT arbeitspreis FROM energy_price WHERE ID_Projekt = ? AND carrier_id = ? ORDER BY valid_from, arbeitspreis",
                new DbParam("@p", projekt), new DbParam("@t", traeger))
            .Rows.Cast<DataRow>().Select(r => Zahl(r[0])).ToList();

        /// <summary>
        /// Wie <see cref="Umbauen"/>, aber je JSON-Abschnitt des Pakets (Projektbäume und
        /// <c>catalogs/</c>) mit dem ganzen Zeilenfeld — Zeilen dürfen dazukommen.
        /// </summary>
        private static void UmbauenMit(string quelle, string ziel, int stand, Action<string, JsonArray> abschnitt)
        {
            var eintraege = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using (ZipArchive zip = ZipFile.OpenRead(quelle))
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    using Stream s = e.Open();
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    eintraege[e.FullName] = ms.ToArray();
                }

            var utf8 = new UTF8Encoding(false);
            using var stream = new FileStream(ziel, FileMode.Create);
            using var aus = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (KeyValuePair<string, byte[]> kvp in eintraege)
            {
                byte[] roh = kvp.Value;
                if (kvp.Key == "manifest.json")
                {
                    JsonObject m = JsonNode.Parse(utf8.GetString(roh)).AsObject();
                    m["schemaVersion"] = stand;
                    roh = utf8.GetBytes(m.ToJsonString());
                }
                else if (kvp.Key.EndsWith(".json", StringComparison.Ordinal))
                {
                    JsonNode n = JsonNode.Parse(utf8.GetString(roh));
                    if (n is JsonArray zeilen)
                    {
                        abschnitt(kvp.Key, zeilen);
                        roh = utf8.GetBytes(zeilen.ToJsonString());
                    }
                }
                using Stream s = aus.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                s.Write(roh, 0, roh.Length);
            }
        }

        // =============================================================================
        //  Handwerkszeug (Stufe 1)
        // =============================================================================

        private static int Id(string projektname) => new ProjektDuplizierenCtrl().GetProjektId(projektname);

        private static long Anzahl(string tabelle) =>
            Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]"), CultureInfo.InvariantCulture);

        private static List<string> Preisbasen(int projekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Preisbasis FROM energy_project_settings WHERE ID_Projekt = ? ORDER BY Preisbasis",
                new DbParam("@p", projekt));
            return dt.Rows.Cast<DataRow>()
                     .Select(r => r[0] == DBNull.Value ? "" : Convert.ToString(r[0], CultureInfo.InvariantCulture))
                     .ToList();
        }

        private static double Zahl(object o)
        {
            if (o is JsonNode n) return n.GetValue<double>();
            return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Schreibt das Paket mit anderem Schemastand neu und lässt jede Zeile jedes
        /// Projektbaums (<c>data/</c>, <c>projects/*/data/</c>) umbauen.
        /// </summary>
        private static void Umbauen(string quelle, string ziel, int stand, Action<string, JsonObject> zeile)
        {
            var eintraege = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            using (ZipArchive zip = ZipFile.OpenRead(quelle))
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    using Stream s = e.Open();
                    using var ms = new MemoryStream();
                    s.CopyTo(ms);
                    eintraege[e.FullName] = ms.ToArray();
                }

            var utf8 = new UTF8Encoding(false);
            using var stream = new FileStream(ziel, FileMode.Create);
            using var aus = new ZipArchive(stream, ZipArchiveMode.Create);
            foreach (KeyValuePair<string, byte[]> kvp in eintraege)
            {
                byte[] roh = kvp.Value;
                if (kvp.Key == "manifest.json")
                {
                    JsonObject m = JsonNode.Parse(utf8.GetString(roh)).AsObject();
                    m["schemaVersion"] = stand;
                    roh = utf8.GetBytes(m.ToJsonString());
                }
                else if (kvp.Key.StartsWith("data/", StringComparison.Ordinal) ||
                         (kvp.Key.StartsWith("projects/", StringComparison.Ordinal) && kvp.Key.Contains("/data/")))
                {
                    string tabelle = Path.GetFileNameWithoutExtension(kvp.Key);
                    JsonArray zeilen = JsonNode.Parse(utf8.GetString(roh)).AsArray();
                    foreach (JsonNode z in zeilen) zeile(tabelle, z.AsObject());
                    roh = utf8.GetBytes(zeilen.ToJsonString());
                }
                using Stream s = aus.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                s.Write(roh, 0, roh.Length);
            }
        }

        /// <summary>Ein Ordner für die Paketdateien eines Falls; er räumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad = Path.Combine(Path.GetTempPath(),
                "epos-anhebung-" + Guid.NewGuid().ToString("N").Substring(0, 8));

            public Arbeitsordner() => Directory.CreateDirectory(_pfad);

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }
    }
}
