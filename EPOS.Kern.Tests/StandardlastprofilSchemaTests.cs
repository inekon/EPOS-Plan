using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt der <b>BDEW-Standardlastprofile Strom 2025</b> (<see cref="StandardlastprofilSchema"/>,
    /// Welle SLP25) auf einer Arbeitskopie der Testdatenbank.
    ///
    /// <para><b>Geprüft wird:</b> die Nummer und die Anweisungen; dass der Schritt drei gesperrte Köpfe in
    /// <c>Tab_Stromverbraucher_STAMM</c> mit je einem gesperrten Typprofil in <c>Tab_Stromverbrauchertyp_STAMM</c>
    /// anlegt, über den Typnamen verknüpft, mit Katalogschlüssel und der Prüfsumme der gelieferten Werte; dass
    /// ein zweiter Lauf nichts ändert und <c>foreign_key_check</c> leer bleibt; dass er nur anlegt, was fehlt,
    /// nie überschreibt und eigene Sätze des Anwenders meldet; dass der Katalogdialog 44 statt 41 Sätze
    /// zeigt; dass eine Projektkopie über <c>ProfilBedarf</c> und <c>BhkwPlan.StromWocheToJahr</c> 8 760
    /// Stundenwerte mit der gepflegten Jahressumme rechnet.</para>
    ///
    /// <para><b>Phase 1:</b> Der Schritt ist noch nicht registriert — die Testdatenbank trägt die Sätze nicht,
    /// jeder Fall führt den Schritt selbst aus. <b>Eigene Arbeitskopie je Fall</b> — die Fälle schreiben.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class StandardlastprofilSchemaTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        private static IReadOnlyList<StandardlastprofilSaat> Saat => StandardlastprofilSchema.Saat;

        /// <summary>Die Sätze des Strombedarfskatalogs in der Testdatenbank vor dem Schritt.</summary>
        private const int KOEPFE_VORHER = 41;

        /// <summary>Referenzprojekt ohne Stromverbraucher-Zuordnung.</summary>
        private const int PROJEKT = 1030;

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>
        /// Die Nummer ist die angemeldete 193 hinter der Kategorie DIN (190) und den Schritten 191/192 der Sitzung
        /// IFC. <b>Phase 1:</b> noch nicht eingehängt — der Zielstand liegt darunter. Phase 2 prüft hier die Kette
        /// (<c>TypaufbauSchema.SCHRITT + 1</c>) und <c>SchemaStand.Zielversion &gt;= SCHRITT</c>.
        /// </summary>
        [Fact]
        public void Die_Nummer_ist_193_und_noch_nicht_eingehaengt()
        {
            Assert.Equal(193, StandardlastprofilSchema.SCHRITT);
            Assert.True(StandardlastprofilSchema.SCHRITT > RaumnutzungDinTsSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion < StandardlastprofilSchema.SCHRITT,
                        "Der Schritt ist eingehängt (Zielstand " + SchemaStand.Zielversion + ") - Phase 2: Kette und Zielstand hier prüfen.");
        }

        /// <summary>Die Anweisungen tragen nur Platzhalter — 170 beim Typprofil, 15 beim Kopf — und die Parameter passen.</summary>
        [Fact]
        public void Die_Anweisungen_tragen_nur_Platzhalter()
        {
            Assert.Equal(170, StandardlastprofilSchema.TypAnweisung().Count(c => c == '?'));
            Assert.Equal(15, StandardlastprofilSchema.KOPF_ANWEISUNG.Count(c => c == '?'));
            Assert.Contains("\"168\", \"ReadOnly\") VALUES", StandardlastprofilSchema.TypAnweisung(), StringComparison.Ordinal);
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.Equal(170, StandardlastprofilSchema.TypParameter(s).Length);
                Assert.Equal(15, StandardlastprofilSchema.KopfParameter(s).Length);
            }
            Assert.Equal(new[] { StandardlastprofilSchema.TAB_TYP, StandardlastprofilSchema.TAB_KOPF },
                         StandardlastprofilSchema.Katalogtabellen().Select(t => t.Tabelle));
        }

        // =============================================================================
        //  Teil 2 - Auf der Arbeitskopie der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Drei gesperrte Köpfe mit den Monatswerten der Konstanten, je ein gesperrtes Typprofil mit den 168
        /// Wochenwerten, verknüpft über den Typnamen; Schlüssel aus dem Namen und die Prüfsumme der gelieferten
        /// Werte (KU1); <c>foreign_key_check</c> leer.
        /// </summary>
        [Fact]
        public void Der_Schritt_saet_drei_gesperrte_Saetze_mit_Typprofil_und_Katalogschluessel()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal((long)KOEPFE_VORHER, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM"));
            long typenVorher = Zahl("SELECT COUNT(*) FROM Tab_Stromverbrauchertyp_STAMM");
            Assert.False(StandardlastprofilSchema.Vollstaendig());

            var bericht = new List<string>();
            StandardlastprofilSchema.Bericht b = StandardlastprofilSchema.Ausfuehren(bericht);
            Assert.Equal(3, b.KoepfeGesaet);
            Assert.Equal(3, b.TypenGesaet);
            Assert.Equal(6, b.Schluessel);
            Assert.Empty(b.Vorhanden);
            Assert.Empty(b.Eigene);
            Assert.Contains(b.Zeile(), bericht);
            Assert.True(StandardlastprofilSchema.Vollstaendig());
            Assert.Equal(KOEPFE_VORHER + 3L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM"));
            Assert.Equal(typenVorher + 3, Zahl("SELECT COUNT(*) FROM Tab_Stromverbrauchertyp_STAMM"));

            Katalogtabelle sv = Katalogfassung.Tabelle(StandardlastprofilSchema.TAB_KOPF);
            Katalogtabelle svt = Katalogfassung.Tabelle(StandardlastprofilSchema.TAB_TYP);
            foreach (StandardlastprofilSaat s in Saat)
            {
                DataRow k = Kopf(s.Bezeichner);
                Assert.Equal(1L, Convert.ToInt64(k["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Typname, Convert.ToString(k["Typ"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Beschreibung, Convert.ToString(k["Beschreibung"], CultureInfo.InvariantCulture));
                var kopfwerte = new Dictionary<string, object>
                {
                    ["Bezeichner"] = s.Bezeichner, ["Typ"] = s.Typname, ["Beschreibung"] = s.Beschreibung,
                };
                for (int m = 0; m < 12; m++)
                {
                    Assert.Equal(s.Monatswerte[m], Convert.ToDouble(k["Monat_" + (m + 1)], CultureInfo.InvariantCulture));
                    kopfwerte["Monat_" + (m + 1)] = s.Monatswerte[m];
                }
                Assert.Equal(Katalogfassung.Schluesselstamm(sv, s.Bezeichner), Convert.ToString(k[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture));
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
                Assert.Equal(Katalogfassung.Schluesselstamm(svt, s.Typname), Convert.ToString(t[Katalogfassung.SPALTE_SCHLUESSEL], CultureInfo.InvariantCulture));
                Assert.Equal(Katalogfassung.Pruefsumme(svt, typwerte), Convert.ToString(t[Katalogfassung.SPALTE_PRUEFSUMME], CultureInfo.InvariantCulture));

                // Die Verknüpfung über den Namen trifft genau dieses Typprofil.
                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM k JOIN Tab_Stromverbrauchertyp_STAMM t " +
                                      "ON t.Typname = k.Typ WHERE k.Bezeichner = ? AND t.ReadOnly = 1", new DbParam("?", s.Bezeichner)));
            }
            Assert.Equal(0, KatalogSchluesselSaat.OffeneSaetze(StandardlastprofilSchema.Katalogtabellen()));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        /// <summary>Ein zweiter Lauf legt nichts an, vergibt keinen Schlüssel und ändert keine Zelle beider Tabellen.</summary>
        [Fact]
        public void Ein_zweiter_Lauf_aendert_nichts()
        {
            if (!_db.Vorhanden) return;

            StandardlastprofilSchema.Ausfuehren(null);
            string vorher = Abzug();

            StandardlastprofilSchema.Bericht b = StandardlastprofilSchema.Ausfuehren(null);
            Assert.Equal(0, b.KoepfeGesaet);
            Assert.Equal(0, b.TypenGesaet);
            Assert.Equal(0, b.Schluessel);
            Assert.Equal(Saat.Select(s => s.Bezeichner), b.Vorhanden);
            Assert.Empty(b.Eigene);
            Assert.Equal(vorher, Abzug());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        /// <summary>
        /// NUR WAS FEHLT, NIE ÜBERSCHREIBEN: Ein eigener Kopf des Anwenders unter „BDEW_G25_Gewerbe" bleibt stehen
        /// (das Typprofil kommt), ein eigenes Typprofil „BDEW_L25" lässt das ganze Profil aus — beides im Protokoll.
        /// Ein gelöschter Kopf kommt beim nächsten Lauf wieder, ein geänderter Wert bleibt geändert.
        /// </summary>
        [Fact]
        public void Der_Schritt_legt_nur_an_was_fehlt_und_meldet_eigene_Saetze()
        {
            if (!_db.Vorhanden) return;

            StandardlastprofilSaat h25 = Saat[0], g25 = Saat[1], l25 = Saat[2];
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO Tab_Stromverbraucher_STAMM (Bezeichner, Typ, Beschreibung, Monat_1, ReadOnly) " +
                                                  "VALUES (?, 'Konst', 'eigen', 5, 0)", new DbParam("?", g25.Bezeichner)));
            Assert.True(DataRepository.ExecuteSQL("INSERT INTO Tab_Stromverbrauchertyp_STAMM (Typname, Beschreibung, \"1\", ReadOnly) " +
                                                  "VALUES (?, 'eigen', 1, 0)", new DbParam("?", l25.Typname)));

            var bericht = new List<string>();
            StandardlastprofilSchema.Bericht b = StandardlastprofilSchema.Ausfuehren(bericht);
            Assert.Equal(1, b.KoepfeGesaet);                                  // nur H25
            Assert.Equal(2, b.TypenGesaet);                                   // H25 und G25
            Assert.Equal(3, b.Schluessel);
            Assert.Equal(new[] { g25.Bezeichner }, b.Vorhanden);
            Assert.Equal(new[] { g25.Bezeichner, l25.Typname }, b.Eigene);
            Assert.Contains(bericht, z => z.Contains("\"" + g25.Bezeichner + "\"", StringComparison.Ordinal));
            Assert.Contains(bericht, z => z.Contains("\"" + l25.Bezeichner + "\"", StringComparison.Ordinal) &&
                                          z.Contains("\"" + l25.Typname + "\"", StringComparison.Ordinal));
            Assert.True(StandardlastprofilSchema.Vollstaendig());

            DataRow g = Kopf(g25.Bezeichner);                                  // eigener Kopf unverändert
            Assert.Equal(0L, Convert.ToInt64(g["ReadOnly"], CultureInfo.InvariantCulture));
            Assert.Equal("Konst", Convert.ToString(g["Typ"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, g[Katalogfassung.SPALTE_SCHLUESSEL]);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Stromverbraucher_STAMM WHERE Bezeichner = ?", new DbParam("?", l25.Bezeichner)));
            Assert.Equal("eigen", Convert.ToString(Typ(l25.Typname)["Beschreibung"], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(Typ(g25.Typname)["ReadOnly"], CultureInfo.InvariantCulture));

            // Gelöscht kommt wieder, geändert bleibt geändert.
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Stromverbraucher_STAMM WHERE Bezeichner = ?", new DbParam("?", h25.Bezeichner)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Stromverbrauchertyp_STAMM SET \"1\" = 9.5 WHERE Typname = ?", new DbParam("?", h25.Typname)));
            Assert.False(StandardlastprofilSchema.Vollstaendig());
            StandardlastprofilSchema.Bericht b2 = StandardlastprofilSchema.Ausfuehren(null);
            Assert.Equal(1, b2.KoepfeGesaet);
            Assert.Equal(0, b2.TypenGesaet);
            Assert.Equal(1, b2.Schluessel);
            Assert.True(StandardlastprofilSchema.Vollstaendig());
            Assert.Equal(h25.Monatswerte[0], Convert.ToDouble(Kopf(h25.Bezeichner)["Monat_1"], CultureInfo.InvariantCulture));
            Assert.Equal(9.5, Convert.ToDouble(Typ(h25.Typname)["1"], CultureInfo.InvariantCulture));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_foreign_key_check"));
        }

        /// <summary>
        /// Der Katalog des Dialogs „Standard Stromprofil" („Datenbank Strombedarf") zeigt nach dem Schritt 44 statt
        /// 41 Sätze; die Jahressumme jedes BDEW-Satzes ist 1.000 MWh, und die Liste der Verwaltung führt dieselben Namen.
        /// </summary>
        [Fact]
        public void Der_Katalogdialog_zeigt_44_Saetze_mit_1000_MWh()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(KOEPFE_VORHER, BedarfStammCtrl.Katalogfilterzeilen(BedarfsArt.Stromverbraucher).Count);
            StandardlastprofilSchema.Ausfuehren(null);

            IReadOnlyList<Katalogfilterzeile> zeilen = BedarfStammCtrl.Katalogfilterzeilen(BedarfsArt.Stromverbraucher);
            Assert.Equal(KOEPFE_VORHER + 3, zeilen.Count);
            IReadOnlyList<string> namen = BedarfStammCtrl.Bezeichner(BedarfsArt.Stromverbraucher);
            Assert.Equal(KOEPFE_VORHER + 3, namen.Count);
            var verwaltung = new StromverbraucherStammCtrl();
            verwaltung.ReadAll();
            Assert.Equal(KOEPFE_VORHER + 3, verwaltung.rows);
            Assert.Equal("BDEW_G25_Gewerbe", verwaltung.items[0].m_szBezeichner);   // ORDER BY Bezeichner: „BD…" vor „Be…"
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.Contains(zeilen, z => z.Bezeichner == s.Bezeichner);
                Assert.Contains(s.Bezeichner, namen);
                Assert.Equal(1000.0, BedarfStammCtrl.Jahressumme(BedarfsArt.Stromverbraucher, s.Bezeichner), 6);
            }

            // Die Dublettenprüfung der Katalogpflege: drei Sätze mehr, keine neue Namens- oder Inhaltsgruppe.
            ScanErgebnis kopf = DublettenPruefung.ScanKatalog(KatalogRegistry.Finde("STROMVERBRAUCHER"));
            Assert.Null(kopf.Fehler);
            Assert.Equal(KOEPFE_VORHER + 3, kopf.Saetze.Count);
            Assert.Empty(kopf.Namensgruppen);
            Assert.Empty(kopf.Inhaltsgruppen);
            ScanErgebnis typ = DublettenPruefung.ScanKatalog(KatalogRegistry.Finde("STROMVERBRAUCHERTYP"));
            Assert.Null(typ.Fehler);
            Assert.Equal(43, typ.Saetze.Count);
            Assert.Empty(typ.Namensgruppen);
            Assert.Single(typ.Inhaltsgruppen);                                         // die Gruppe des Bestands
        }

        /// <summary>
        /// Die Projektkopie rechnet: Der Assistent übernimmt die drei Sätze mit gepflegter Jahressumme (4,5, 120 und
        /// 30 MWh) in ein Projekt ohne Stromverbraucher; Kopf und Typprofil stehen als Kopie im Projekt, und der Lauf
        /// rechnet über <c>ProfilBedarf</c> und <c>BhkwPlan.StromWocheToJahr</c> 8 760 Stundenwerte, deren Jahres- und
        /// Monatssummen die gepflegten Summen nach den Monatswerten der Sätze verteilen.
        /// </summary>
        [Fact]
        public void Die_Projektkopie_rechnet_8760_Stunden_mit_der_gepflegten_Jahressumme()
        {
            if (!_db.Vorhanden) return;

            StandardlastprofilSchema.Ausfuehren(null);
            Assert.Empty(Z_ProjektStromverbraucherCtrl.LiesProjekt(PROJEKT));

            double[] summen = { 4.5, 120.0, 30.0 };
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
            for (int m = 0; m < 12; m++)
            {
                double ist = reihe.Skip(stunde).Take(24 * tage[m]).Sum();
                double soll = Enumerable.Range(0, Saat.Count).Sum(i => Saat[i].Monatswerte[m] * summen[i]);   // MWh/1000 MWh x Summe x 1000 kWh
                Nah(soll, ist, 1e-9, "Monat " + (m + 1) + " [kWh]");
                stunde += 24 * tage[m];
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

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
