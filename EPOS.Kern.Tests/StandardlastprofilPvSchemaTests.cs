using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt der <b>BDEW-Netzbezugsprofile P25 und S25</b> (<see cref="StandardlastprofilPvSchema"/>,
    /// Welle SLP25b) auf einer Arbeitskopie der Testdatenbank.
    ///
    /// <para><b>Geprüft wird:</b> die Kette (<c>ErdsondenfeldSchema.SCHRITT + 1</c>), der Zielstand, die Stufe der
    /// Paketanhebung und die Saat — die zwei Netzbezugsprofile, getrennt von den Verbrauchsprofilen, mit derselben
    /// Mechanik wie Schritt 193; dass der Schritt zwei gesperrte Köpfe
    /// in <c>Tab_Stromverbraucher_STAMM</c> mit je einem gesperrten Typprofil in <c>Tab_Stromverbrauchertyp_STAMM</c>
    /// anlegt (46 statt 44 Köpfe, 45 statt 43 Typprofile), über den Typnamen verknüpft, mit Katalogschlüssel und der
    /// Prüfsumme der gelieferten Werte, ohne die Sätze H25, G25, L25 anzufassen; dass ein zweiter Lauf nichts ändert und
    /// <c>foreign_key_check</c> leer bleibt; dass er nur anlegt, was fehlt, nie überschreibt und eigene Sätze des
    /// Anwenders meldet; dass der Katalogdialog 46 Sätze zeigt; dass eine Projektkopie über <c>ProfilBedarf</c> und
    /// <c>BhkwPlan.StromWocheToJahr</c> 8 760 Stundenwerte mit dem gepflegten Jahresnetzbezug rechnet; dass Repo-Datei,
    /// Werkzeug, Migration und Testkopie den Schritt hinter 195 führen.</para>
    ///
    /// <para><b>Vom gelieferten Stand aus:</b> Die Testdatenbank trägt die zwei Sätze. Fälle, die das Säen prüfen,
    /// löschen sie zuerst (<see cref="SaetzeLoeschen"/>) und säen dann; die übrigen führen den Schritt vorab aus, der
    /// dann nichts findet. <b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StandardlastprofilPvSchemaTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private static IReadOnlyList<StandardlastprofilSaat> Saat => StandardlastprofilPvSchema.Saat;

        /// <summary>Die Köpfe des Strombedarfskatalogs ohne P25 und S25 (41 und die drei Verbrauchsprofile des Schritts 193).</summary>
        private const int KOEPFE_OHNE = 44;

        /// <summary>Die Typprofile des Strombedarfskatalogs ohne P25 und S25 (40 und die drei des Schritts 193).</summary>
        private const int TYPEN_OHNE = 43;

        /// <summary>Referenzprojekt ohne Stromverbraucher-Zuordnung.</summary>
        private const int PROJEKT = 1030;

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>
        /// Die Nummer folgt lückenlos auf das Erdsondenfeld (195, Sitzung Dialoge); der Zielstand reicht bis zu ihr, und
        /// das Register der Paketanhebung führt sie als reinen Katalogschritt.
        /// </summary>
        [Fact]
        public void Die_Nummer_folgt_auf_das_Erdsondenfeld_und_ist_das_Ziel()
        {
            Assert.Equal(ErdsondenfeldSchema.SCHRITT + 1, StandardlastprofilPvSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= StandardlastprofilPvSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + StandardlastprofilPvSchema.SCHRITT + ".");
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == StandardlastprofilPvSchema.SCHRITT &&
                                                       s.Wirkung == Paketanhebung.Art.Katalog);
        }

        /// <summary>
        /// Die Saat sind die zwei Netzbezugsprofile P25 und S25, getrennt von den drei Verbrauchsprofilen des Schritts 193;
        /// Tabellen, Voraussetzungen und Anweisungen sind dieselben — EINE Mechanik.
        /// </summary>
        [Fact]
        public void Die_Saat_sind_die_zwei_Netzbezugsprofile_mit_derselben_Mechanik()
        {
            Assert.Equal(new[] { "P25", "S25" }, Saat.Select(s => s.Kuerzel));
            Assert.Same(StandardlastprofilSaattabelle.Netzbezug, Saat);
            Assert.Empty(Saat.Select(s => s.Bezeichner).Intersect(StandardlastprofilSchema.Saat.Select(s => s.Bezeichner)));
            Assert.Empty(Saat.Select(s => s.Typname).Intersect(StandardlastprofilSchema.Saat.Select(s => s.Typname)));
            Assert.Equal(StandardlastprofilSchema.Voraussetzungen(), StandardlastprofilPvSchema.Voraussetzungen());
            Assert.Equal(new[] { StandardlastprofilSchema.TAB_TYP, StandardlastprofilSchema.TAB_KOPF },
                         StandardlastprofilPvSchema.Katalogtabellen().Select(t => t.Tabelle));
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.Equal(170, StandardlastprofilSchema.TypParameter(s).Length);
                Assert.Equal(15, StandardlastprofilSchema.KopfParameter(s).Length);
            }
        }

        // =============================================================================
        //  Teil 2 - Auf der Arbeitskopie der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Zwei gesperrte Köpfe mit den Monatswerten der Konstanten, je ein gesperrtes Typprofil mit den 168 Wochenwerten,
        /// verknüpft über den Typnamen; Schlüssel aus dem Namen (<c>SV:BDEW_P25_HAUSHALT_PV</c>, <c>SVT:BDEW_P25</c> …) und
        /// die Prüfsumme der gelieferten Werte; <c>foreign_key_check</c> leer. Die drei Verbrauchsprofile bleiben Zelle für
        /// Zelle, wie sie waren, und ihr Schritt findet danach nichts zu tun.
        /// </summary>
        [Fact]
        public void Der_Schritt_saet_zwei_gesperrte_Saetze_mit_Typprofil_und_Katalogschluessel()
        {
            if (!_db.Vorhanden) return;

            SaetzeLoeschen();
            Assert.True(StandardlastprofilSchema.Vollstaendig());
            Assert.False(StandardlastprofilPvSchema.Vollstaendig());
            Assert.Equal((long)KOEPFE_OHNE, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM"));
            Assert.Equal((long)TYPEN_OHNE, Zahl("SELECT COUNT(*) FROM Tab_Stromverbrauchertyp_STAMM"));
            string verbrauchVorher = Zeilen(StandardlastprofilSchema.Saat);

            var bericht = new List<string>();
            StandardlastprofilSchema.Bericht b = StandardlastprofilPvSchema.Ausfuehren(bericht);
            Assert.Equal(2, b.KoepfeGesaet);
            Assert.Equal(2, b.TypenGesaet);
            Assert.Equal(4, b.Schluessel);
            Assert.Empty(b.Vorhanden);
            Assert.Empty(b.Eigene);
            Assert.Contains(b.Zeile(), bericht);
            Assert.StartsWith("2 von 2 BDEW-Netzbezugsprofil(e) P25/S25 gesaet (ReadOnly = 1), 2 Typprofil(e), 4 Satz/Saetze",
                              b.Zeile(), StringComparison.Ordinal);
            Assert.True(StandardlastprofilPvSchema.Vollstaendig());
            Assert.Equal(KOEPFE_OHNE + 2L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM"));
            Assert.Equal(TYPEN_OHNE + 2L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbrauchertyp_STAMM"));

            Katalogtabelle sv = Katalogfassung.Tabelle(StandardlastprofilSchema.TAB_KOPF);
            Katalogtabelle svt = Katalogfassung.Tabelle(StandardlastprofilSchema.TAB_TYP);
            string[] kopfSchluessel = { "SV:BDEW_P25_HAUSHALT_PV", "SV:BDEW_S25_HAUSHALT_PV_SPEICHER" };
            string[] typSchluessel = { "SVT:BDEW_P25", "SVT:BDEW_S25" };
            for (int i = 0; i < Saat.Count; i++)
            {
                StandardlastprofilSaat s = Saat[i];
                DataRow k = Kopf(s.Bezeichner);
                Assert.Equal(1L, Convert.ToInt64(k["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Typname, Convert.ToString(k["Typ"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Beschreibung, Convert.ToString(k["Beschreibung"], CultureInfo.InvariantCulture));
                Assert.Contains("kein Verbrauchsprofil", Convert.ToString(k["Beschreibung"], CultureInfo.InvariantCulture), StringComparison.Ordinal);
                var kopfwerte = new Dictionary<string, object>
                {
                    ["Bezeichner"] = s.Bezeichner, ["Typ"] = s.Typname, ["Beschreibung"] = s.Beschreibung,
                };
                for (int m = 0; m < 12; m++)
                {
                    Assert.Equal(s.Monatswerte[m], Convert.ToDouble(k["Monat_" + (m + 1)], CultureInfo.InvariantCulture));
                    kopfwerte["Monat_" + (m + 1)] = s.Monatswerte[m];
                }
                Assert.Equal(kopfSchluessel[i], Katalogfassung.Schluesselstamm(sv, s.Bezeichner));
                Assert.Equal(kopfSchluessel[i], Convert.ToString(k[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture));
                Assert.Equal(Katalogfassung.Pruefsumme(sv, kopfwerte), Convert.ToString(k[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture));

                DataRow t = Typ(s.Typname);
                Assert.Equal(1L, Convert.ToInt64(t["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Typbeschreibung, Convert.ToString(t["Beschreibung"], CultureInfo.InvariantCulture));
                var typwerte = new Dictionary<string, object> { ["Typname"] = s.Typname, ["Beschreibung"] = s.Typbeschreibung };
                for (int h = 0; h < 168; h++)
                {
                    string spalte = (h + 1).ToString(CultureInfo.InvariantCulture);
                    Assert.Equal(s.Wochenwerte[h], Convert.ToDouble(t[spalte], CultureInfo.InvariantCulture));
                    typwerte[spalte] = s.Wochenwerte[h];
                }
                Assert.Equal(typSchluessel[i], Katalogfassung.Schluesselstamm(svt, s.Typname));
                Assert.Equal(typSchluessel[i], Convert.ToString(t[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture));
                Assert.Equal(Katalogfassung.Pruefsumme(svt, typwerte), Convert.ToString(t[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture));

                // Die Verknüpfung über den Namen trifft genau dieses Typprofil.
                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM k JOIN Tab_Stromverbrauchertyp_STAMM t " +
                                      "ON t.Typname = k.Typ WHERE k.Bezeichner = ? AND t.ReadOnly = 1", new DbParam("?", s.Bezeichner)));
            }
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(StandardlastprofilPvSchema.Katalogtabellen()));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));

            // Die Verbrauchsprofile bleiben, wie sie waren; ihr Schritt findet nichts zu tun.
            Assert.Equal(verbrauchVorher, Zeilen(StandardlastprofilSchema.Saat));
            StandardlastprofilSchema.Bericht v = StandardlastprofilSchema.Ausfuehren(null);
            Assert.Equal(0, v.KoepfeGesaet + v.TypenGesaet + v.Schluessel);
        }

        /// <summary>
        /// Ein Lauf auf dem gesäten Stand legt nichts an, vergibt keinen Schlüssel und ändert keine Zelle beider Tabellen.
        /// </summary>
        [Fact]
        public void Ein_zweiter_Lauf_aendert_nichts()
        {
            if (!_db.Vorhanden) return;

            StandardlastprofilPvSchema.Ausfuehren(null);
            Assert.True(StandardlastprofilPvSchema.Vollstaendig());
            string vorher = Abzug();

            StandardlastprofilSchema.Bericht b = StandardlastprofilPvSchema.Ausfuehren(null);
            Assert.Equal(0, b.KoepfeGesaet);
            Assert.Equal(0, b.TypenGesaet);
            Assert.Equal(0, b.Schluessel);
            Assert.Equal(Saat.Select(s => s.Bezeichner), b.Vorhanden);
            Assert.Empty(b.Eigene);
            Assert.Equal(vorher, Abzug());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        /// <summary>
        /// NUR WAS FEHLT, NIE ÜBERSCHREIBEN: Ein eigener Kopf des Anwenders unter „BDEW_P25_Haushalt_PV" bleibt stehen (das
        /// Typprofil kommt), ein eigenes Typprofil „BDEW_S25" lässt das ganze Profil aus — beides im Protokoll. Ein fehlender
        /// Kopf kommt beim nächsten Lauf, ein geänderter Wert bleibt geändert.
        /// </summary>
        [Fact]
        public void Der_Schritt_legt_nur_an_was_fehlt_und_meldet_eigene_Saetze()
        {
            if (!_db.Vorhanden) return;

            StandardlastprofilSaat p25 = Saat[0], s25 = Saat[1];
            SaetzeLoeschen();
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO Tab_Stromverbraucher_STAMM (Bezeichner, Typ, Beschreibung, Monat_1, ReadOnly) " +
                                                  "VALUES (?, 'Konst', 'eigen', 5, 0)", new DbParam("?", p25.Bezeichner)));
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO Tab_Stromverbrauchertyp_STAMM (Typname, Beschreibung, \"1\", ReadOnly) " +
                                                  "VALUES (?, 'eigen', 1, 0)", new DbParam("?", s25.Typname)));

            var bericht = new List<string>();
            StandardlastprofilSchema.Bericht b = StandardlastprofilPvSchema.Ausfuehren(bericht);
            Assert.Equal(0, b.KoepfeGesaet);                                  // P25 steht als eigener Kopf, S25 entfällt
            Assert.Equal(1, b.TypenGesaet);                                   // nur BDEW_P25
            Assert.Equal(1, b.Schluessel);
            Assert.Equal(new[] { p25.Bezeichner }, b.Vorhanden);
            Assert.Equal(new[] { p25.Bezeichner, s25.Typname }, b.Eigene);
            Assert.Contains(bericht, z => z.Contains("\"" + p25.Bezeichner + "\"", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("\"" + s25.Bezeichner + "\"", StringComparison.Ordinal) &&
                                          z.Contains("\"" + s25.Typname + "\"", StringComparison.Ordinal));
            Assert.True(StandardlastprofilPvSchema.Vollstaendig());

            DataRow p = Kopf(p25.Bezeichner);                                  // eigener Kopf unverändert
            Assert.Equal(0L, Convert.ToInt64(p["ReadOnly"], CultureInfo.InvariantCulture));
            Assert.Equal("Konst", Convert.ToString(p["Typ"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, p[Katalogfassung.SPALTE_SCHLUESSEL]);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM WHERE Bezeichner = ?", new DbParam("?", s25.Bezeichner)));
            Assert.Equal("eigen", Convert.ToString(Typ(s25.Typname)["Beschreibung"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(Typ(p25.Typname)["ReadOnly"], CultureInfo.InvariantCulture));

            // Fehlt der Kopf, kommt er; geändert bleibt geändert.
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Stromverbraucher_STAMM WHERE Bezeichner = ?", new DbParam("?", p25.Bezeichner)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Stromverbrauchertyp_STAMM SET \"1\" = 9.5 WHERE Typname = ?", new DbParam("?", p25.Typname)));
            Assert.False(StandardlastprofilPvSchema.Vollstaendig());
            StandardlastprofilSchema.Bericht b2 = StandardlastprofilPvSchema.Ausfuehren(null);
            Assert.Equal(1, b2.KoepfeGesaet);
            Assert.Equal(0, b2.TypenGesaet);
            Assert.Equal(1, b2.Schluessel);
            Assert.True(StandardlastprofilPvSchema.Vollstaendig());
            DataRow neu = Kopf(p25.Bezeichner);
            Assert.Equal(1L, Convert.ToInt64(neu["ReadOnly"], CultureInfo.InvariantCulture));
            Assert.Equal(p25.Monatswerte[0], Convert.ToDouble(neu["Monat_1"], CultureInfo.InvariantCulture));
            Assert.Equal(9.5, Convert.ToDouble(Typ(p25.Typname)["1"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        /// <summary>
        /// Der Katalog des Dialogs „Standard Stromprofil" („Datenbank Strombedarf") zeigt mit P25 und S25 46 statt 44 Sätze;
        /// die Jahressumme beider ist 1.000 MWh, die Liste der Verwaltung führt dieselben Namen, und die Dublettenprüfung der
        /// Katalogpflege findet keine neue Namens- oder Inhaltsgruppe (45 Typprofile, die eine Inhaltsgruppe des Bestands).
        /// </summary>
        [Fact]
        public void Der_Katalogdialog_zeigt_46_Saetze_mit_1000_MWh()
        {
            if (!_db.Vorhanden) return;

            StandardlastprofilPvSchema.Ausfuehren(null);
            Assert.True(StandardlastprofilPvSchema.Vollstaendig());

            IReadOnlyList<Katalogfilterzeile> zeilen = BedarfStammCtrl.Katalogfilterzeilen(BedarfsArt.Stromverbraucher);
            Assert.Equal(KOEPFE_OHNE + 2, zeilen.Count);
            IReadOnlyList<string> namen = BedarfStammCtrl.Bezeichner(BedarfsArt.Stromverbraucher);
            Assert.Equal(KOEPFE_OHNE + 2, namen.Count);
            var verwaltung = new StromverbraucherStammCtrl();
            verwaltung.ReadAll();
            Assert.Equal(KOEPFE_OHNE + 2, verwaltung.rows);
            Assert.Equal("BDEW_G25_Gewerbe", verwaltung.items[0].m_szBezeichner);   // ORDER BY Bezeichner: „BDEW_G…" bleibt vorn
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.Contains(zeilen, z => z.Bezeichner == s.Bezeichner);
                Assert.Contains(s.Bezeichner, namen);
                Assert.Equal(1000.0, BedarfStammCtrl.Jahressumme(BedarfsArt.Stromverbraucher, s.Bezeichner), 6);
            }

            ScanErgebnis kopf = DublettenPruefung.ScanKatalog(KatalogRegistry.Finde("STROMVERBRAUCHER"));
            Assert.Null(kopf.Fehler);
            Assert.Equal(KOEPFE_OHNE + 2, kopf.Saetze.Count);
            Assert.Empty(kopf.Namensgruppen);
            Assert.Empty(kopf.Inhaltsgruppen);
            ScanErgebnis typ = DublettenPruefung.ScanKatalog(KatalogRegistry.Finde("STROMVERBRAUCHERTYP"));
            Assert.Null(typ.Fehler);
            Assert.Equal(TYPEN_OHNE + 2, typ.Saetze.Count);
            Assert.Empty(typ.Namensgruppen);
            Assert.Single(typ.Inhaltsgruppen);                                         // die Gruppe des Bestands
        }

        /// <summary>
        /// Die Projektkopie rechnet: Der Assistent übernimmt P25 und S25 mit gepflegtem Jahresnetzbezug (3,5 und 2 MWh) in
        /// ein Projekt ohne Stromverbraucher; Kopf und Typprofil stehen als Kopie im Projekt, und der Lauf rechnet über
        /// <c>ProfilBedarf</c> und <c>BhkwPlan.StromWocheToJahr</c> 8 760 Stundenwerte, deren Jahres- und Monatssummen die
        /// gepflegten Summen nach den Monatswerten der Sätze verteilen — im Juni wenig, im Winter viel.
        /// </summary>
        [Fact]
        public void Die_Projektkopie_rechnet_8760_Stunden_mit_dem_gepflegten_Jahresnetzbezug()
        {
            if (!_db.Vorhanden) return;

            StandardlastprofilPvSchema.Ausfuehren(null);
            Assert.True(StandardlastprofilPvSchema.Vollstaendig());
            Assert.Empty(Z_ProjektStromverbraucherCtrl.LiesProjekt(PROJEKT));

            double[] summen = { 3.5, 2.0 };
            var liste = new List<Z_ProjektStromverbraucherModel>();
            for (int i = 0; i < Saat.Count; i++)
                liste.Add(new Z_ProjektStromverbraucherModel { m_ID_Projekt = PROJEKT, m_szVerbraucher = Saat[i].Bezeichner, m_Summe = summen[i] });
            Assert.True(new WizardCtrl().Add_Projekt_Stromverbraucher(PROJEKT, liste));

            foreach (StandardlastprofilSaat s in Saat)
            {
                DataTable kopie = DataRepository.GetDataTable(
                    "SELECT ID, Typ FROM Tab_Stromverbraucher WHERE ID_Projekt = ? AND Bezeichner = ?",
                    new DbParam("?", PROJEKT), new DbParam("?", s.Bezeichner));
                Assert.Equal(1, kopie.Rows.Count);
                Assert.Equal(s.Typname, Convert.ToString(kopie.Rows[0]["Typ"], CultureInfo.InvariantCulture));
                DataTable typ = DataRepository.GetDataTable("SELECT * FROM Tab_Stromverbrauchertyp WHERE ID_Stromverbraucher = ?",
                                                            new DbParam("?", Convert.ToInt64(kopie.Rows[0]["ID"], CultureInfo.InvariantCulture)));
                Assert.Equal(1, typ.Rows.Count);
                for (int h = 0; h < 168; h++)
                    Assert.Equal(s.Wochenwerte[h], Convert.ToDouble(typ.Rows[0][(h + 1).ToString(CultureInfo.InvariantCulture)], CultureInfo.InvariantCulture));
            }

            double[] reihe = new SimulationStrombedarf { m_ID_Projekt = PROJEKT }.Stromprofil_Strombedarf_berechnen();
            Assert.NotNull(reihe);
            Assert.Equal(ProfilBedarf.STUNDEN_JAHR, reihe.Length);
            Assert.All(reihe, x => Assert.True(double.IsFinite(x) && x > 0));
            Nah(summen.Sum() * 1000.0, reihe.Sum(), 1e-9, "Jahressumme [kWh]");

            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int stunde = 0;
            var monate = new double[12];
            for (int m = 0; m < 12; m++)
            {
                monate[m] = reihe.Skip(stunde).Take(24 * tage[m]).Sum();
                double soll = Enumerable.Range(0, Saat.Count).Sum(i => Saat[i].Monatswerte[m] * summen[i]);   // MWh/1000 MWh x Summe x 1000 kWh
                Nah(soll, monate[m], 1e-9, "Monat " + (m + 1) + " [kWh]");
                stunde += 24 * tage[m];
            }
            Assert.True(monate[5] < 0.05 * monate.Sum() && monate[11] > 3 * monate[5], "Juni nicht klein gegen Dezember");
        }

        // =============================================================================
        //  Teil 3 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und Testkopie führen den
        /// Schritt aus derselben Quelle NACH dem Erdsondenfeld (195); die Repo-Datei trägt ihn — Schemastand und die zwei
        /// gesperrten Sätze samt Typprofil und Katalogschlüssel (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wSaat = werkzeug.IndexOf("StandardlastprofilPvSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wSaat > 0 && wSaat > werkzeug.IndexOf("ErdsondenfeldSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Die Saat steht im Werkzeug nicht hinter dem Erdsondenfeld.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_STANDARDLASTPROFIL_PV = StandardlastprofilPvSchema.SCHRITT", migration, StringComparison.Ordinal);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_ERDSONDENFELD", StringComparison.Ordinal);
            int ortSaat = migration.IndexOf("new Schritt(SCHRITT_STANDARDLASTPROFIL_PV", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSaat > ortVorher, "Der Schritt steht nicht hinter 195.");
            Assert.Contains("StandardlastprofilPvSchema.Ausfuehren(bericht)", migration, StringComparison.Ordinal);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vSaat = vorrichtung.IndexOf("StandardlastprofilPvSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vSaat > 0 && vSaat > vorrichtung.IndexOf("ErdsondenfeldSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Die Saat steht in der Testkopie nicht hinter dem Erdsondenfeld.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SchemaVersion FROM Tab_Applikation";
            Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) >= StandardlastprofilPvSchema.SCHRITT);
            cmd.CommandText = "SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM k JOIN Tab_Stromverbrauchertyp_STAMM t ON t.Typname = k.Typ " +
                              "WHERE k.Bezeichner = $b AND t.Typname = $t AND k.ReadOnly = 1 AND t.ReadOnly = 1 " +
                              "AND k.Katalog_Schluessel IS NOT NULL AND t.Katalog_Schluessel IS NOT NULL";
            foreach (StandardlastprofilSaat s in Saat)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("$b", s.Bezeichner);
                cmd.Parameters.AddWithValue("$t", s.Typname);
                Assert.Equal(1L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }

        /// <summary>Löscht P25 und S25 (Köpfe und Typprofile) unter ihren Namen — der Stand vor dem Schritt.</summary>
        private static void SaetzeLoeschen()
        {
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Stromverbraucher_STAMM WHERE Bezeichner = ?", new DbParam("?", s.Bezeichner)));
                Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Stromverbrauchertyp_STAMM WHERE Typname = ?", new DbParam("?", s.Typname)));
            }
        }

        private static void Nah(double erwartet, double ist, double relativ, string was)
            => Assert.True(Math.Abs(erwartet - ist) <= relativ * Math.Max(1.0, Math.Abs(erwartet)),
                           was + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist "
                           + ist.ToString("R", CultureInfo.InvariantCulture));

        private static long Zahl(string sql, params DbParam[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p), CultureInfo.InvariantCulture);

        private static DataRow Kopf(string bezeichner) => Einzig("Tab_Stromverbraucher_STAMM", "Bezeichner", bezeichner);

        private static DataRow Typ(string typname) => Einzig("Tab_Stromverbrauchertyp_STAMM", "Typname", typname);

        private static DataRow Einzig(string tabelle, string spalte, string name)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" = ?", new DbParam("?", name));
            Assert.True(dt != null && dt.Rows.Count == 1, "Kein eindeutiger Satz: " + tabelle + " " + name);
            return dt.Rows[0];
        }

        /// <summary>Kopf und Typprofil der genannten Sätze, jede Zelle als Text.</summary>
        private static string Zeilen(IEnumerable<StandardlastprofilSaat> saat)
        {
            var sb = new StringBuilder();
            foreach (StandardlastprofilSaat s in saat)
                foreach (DataRow r in new[] { Kopf(s.Bezeichner), Typ(s.Typname) })
                {
                    foreach (DataColumn c in r.Table.Columns)
                        sb.Append(c.ColumnName).Append('=').Append(Convert.ToString(r[c], CultureInfo.InvariantCulture)).Append('|');
                    sb.Append('\n');
                }
            return sb.ToString();
        }

        /// <summary>Jede Zelle beider Kataloge und ihre Zähler in <c>sqlite_sequence</c>, als Text.</summary>
        private static string Abzug()
        {
            var sb = new StringBuilder();
            foreach (string tabelle in new[] { StandardlastprofilSchema.TAB_KOPF, StandardlastprofilSchema.TAB_TYP })
            {
                DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" ORDER BY ID");
                foreach (DataRow r in dt.Rows)
                {
                    foreach (DataColumn c in dt.Columns)
                        sb.Append(c.ColumnName).Append('=').Append(Convert.ToString(r[c], CultureInfo.InvariantCulture)).Append('|');
                    sb.Append('\n');
                }
                sb.Append(tabelle).Append(" seq=").Append(Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT seq FROM sqlite_sequence WHERE name = ?", new DbParam("?", tabelle)), CultureInfo.InvariantCulture)).Append('\n');
            }
            return sb.ToString();
        }
    }
}
