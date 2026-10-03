using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Projektkopien der Brennstoffe, Konditionierungsvorlagen und Pufferauslegungs-Vorgaben</b>
    /// (Anwenderentscheid 03.10.2026, Schemaschritt <see cref="ProjektkopienKatalogeSchema"/>, Konzept
    /// Simulationsablauf Abschnitt 22) — auf einer Arbeitskopie der Testdatenbank: Die Saat kopiert
    /// wertgleich und ist wiederholbar, das Projekt liest seine Kopie, der Katalogabgleich ändert nur
    /// den Stamm, und die Projekte rechnen danach wie vorher; Übernehmen, Zurücksetzen und Bearbeiten
    /// des Projektdialogs; Duplizieren und Projektpaket tragen die Kopien.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ProjektkopienKatalogeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die sechzehn Projekte der Referenzbasis.</summary>
        private static readonly int[] REFERENZPROJEKTE =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047, 1049, 1050 };

        /// <summary>Erdgas E — der Brennstoff des Kessels und des Trägers von 1030.</summary>
        private const int ERDGAS_E = 3;

        /// <summary>Der Träger „Erdgas E" von 1030.</summary>
        private const int TRAEGER_1030 = 63;

        private const string FACHSPALTEN =
            "ID_Kategorie, Bezeichner, Einheit, PreisEinheit, Hi, Hs, CO2, SO2, NOx, Staub, PE_Faktor, " +
            "Standard_Grundpreis, Standard_Arbeitspreis, Standard_Leistungspreis";

        // =====================================================================
        //  Schritt, Register, Schema
        // =====================================================================

        [Fact]
        public void Nummer_Ziel_und_Paketstufe()
        {
            Assert.Equal(AufheizManuellSchema.SCHRITT + 1, ProjektkopienKatalogeSchema.SCHRITT);
            Assert.Equal(175, ProjektkopienKatalogeSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ProjektkopienKatalogeSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= ProjektkopienKatalogeSchema.SCHRITT);
            Assert.Equal(Paketanhebung.Art.Ddl,
                         Paketanhebung.Stufen.Single(x => x.Nr == ProjektkopienKatalogeSchema.SCHRITT).Wirkung);
        }

        [Fact]
        public void Beide_Tabellen_sind_STRICT_und_je_Projekt_eindeutig()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ProjektkopienKatalogeSchema.Vollstaendig());
            foreach (string t in new[] { ProjektkopienKatalogeSchema.TAB_BRENNSTOFF, ProjektkopienKatalogeSchema.TAB_PUFFERPARAMETER })
            {
                string sql = Text("SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '" + t + "'");
                Assert.EndsWith(") STRICT", sql.TrimEnd());
                Assert.Contains("ON DELETE CASCADE", sql);
                Assert.Contains(ProjektkopienKatalogeSchema.SPALTE_HERKUNFT, sql);
            }
            Assert.True(DuplikatEinfuegen() <= 0, "Die zweite Kopie derselben Brennstoffart ist abzulehnen.");
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM " + ProjektkopienKatalogeSchema.TAB_BRENNSTOFF +
                                  " WHERE ID_Projekt NOT IN (SELECT ID FROM Tab_Projekt)"));
        }

        private static int DuplikatEinfuegen()
        {
            try
            {
                using (DataRepository.EngineModus())
                    return DataRepository.ExecuteNonQuery(
                        "INSERT INTO Tab_Brennstoff (ID_Projekt, ID_Brennstoff, ID_Kategorie, Bezeichner) VALUES (1030, 3, 1, 'Doppelt')");
            }
            catch (Exception)
            {
                return -1;
            }
            finally
            {
                DataRepository.StilleFehlerAbholen();
            }
        }

        // =====================================================================
        //  Saat: wertgleich, vollständig, wiederholbar
        // =====================================================================

        [Fact]
        public void Saat_kopiert_jeden_Brennstoff_jedes_Projekts_wertgleich()
        {
            if (!_db.Vorhanden) return;
            long stamm = Zahl("SELECT COUNT(*) FROM Tab_Brennstoff_Stamm");
            long projekte = Zahl("SELECT COUNT(*) FROM Tab_Projekt");
            Assert.True(stamm > 0);
            Assert.Equal(stamm * projekte, Zahl("SELECT COUNT(*) FROM Tab_Brennstoff"));
            Assert.Equal(0, ProjektBrennstoffe.OffeneKopien());

            foreach (int p in REFERENZPROJEKTE)
            {
                Assert.Equal(stamm, Zahl("SELECT COUNT(*) FROM Tab_Brennstoff WHERE ID_Projekt = " + p));
                Assert.Equal(Bild("SELECT ID AS Art, " + FACHSPALTEN + " FROM Tab_Brennstoff_Stamm ORDER BY ID"),
                             Bild("SELECT ID_Brennstoff AS Art, " + FACHSPALTEN + " FROM Tab_Brennstoff WHERE ID_Projekt = " + p +
                                  " ORDER BY ID_Brennstoff"));
            }
            // Typgleich, nicht nur wertgleich (STRICT REAL gegen REAL, NULL bleibt NULL).
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Brennstoff k JOIN Tab_Brennstoff_Stamm s ON s.ID = k.ID_Brennstoff " +
                                  "WHERE typeof(k.CO2) <> typeof(s.CO2) OR typeof(k.Hi) <> typeof(s.Hi) OR typeof(k.Staub) <> typeof(s.Staub)"));
        }

        [Fact]
        public void Ein_zweiter_Lauf_tut_nichts()
        {
            if (!_db.Vorhanden) return;
            string vorher = Bild("SELECT * FROM Tab_Brennstoff ORDER BY ID") + Bild("SELECT * FROM Tab_PufferAuslegungParameter ORDER BY ID");
            var bericht = new List<string>();
            Assert.Equal(0, ProjektkopienKatalogeSchema.Ausfuehren(bericht));
            Assert.Contains(bericht, z => z.Contains(": 0 Projektkopie(n) angelegt", StringComparison.Ordinal));
            Assert.Equal(vorher, Bild("SELECT * FROM Tab_Brennstoff ORDER BY ID") + Bild("SELECT * FROM Tab_PufferAuslegungParameter ORDER BY ID"));
        }

        [Fact]
        public void Saat_der_Vorgaben_nur_fuer_Projekte_mit_Auslegung()
        {
            if (!_db.Vorhanden) return;
            long vorgaben = Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegungParameter_STAMM");
            long mitAuslegung = Zahl("SELECT COUNT(DISTINCT ID_Projekt) FROM Tab_PufferAuslegung");
            Assert.Equal(vorgaben * mitAuslegung, Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegungParameter"));

            DataRepository.ExecuteNonQuery("INSERT INTO Tab_PufferAuslegung (ID_Projekt) VALUES (1045)");
            Assert.Equal((int)vorgaben, ProjektkopienKatalogeSchema.Saat(null));
            Assert.Equal(Bild("SELECT Schluessel, Wert, Einheit, Quelle, Herkunftsart FROM Tab_PufferAuslegungParameter_STAMM ORDER BY Schluessel"),
                         Bild("SELECT Schluessel, Wert, Einheit, Quelle, Herkunftsart FROM Tab_PufferAuslegungParameter " +
                              "WHERE ID_Projekt = 1045 ORDER BY Schluessel"));
            Assert.Equal(0, ProjektPufferparameter.OffeneKopien());
        }

        // =====================================================================
        //  Lesen: die Sicht des Projekts
        // =====================================================================

        [Fact]
        public void Die_Sicht_liest_die_Kopie_und_fuer_eine_unbekannte_Art_den_Katalog()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Brennstoff SET CO2 = 111 WHERE ID_Projekt = 1030 AND ID_Brennstoff = 3");
            DataRepository.ExecuteNonQuery("UPDATE Tab_Brennstoff_Stamm SET CO2 = 222 WHERE ID IN (3, 4)");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Brennstoff WHERE ID_Projekt = 1030 AND ID_Brennstoff = 4");

            Assert.Equal(111.0, Co2DerSicht(1030, 3));
            Assert.Equal(222.0, Co2DerSicht(1030, 4));      // dem Projekt unbekannt: der Katalog
            Assert.Equal(222.0, Co2DerSicht(0, 3));         // ohne Projekt: der Katalog
            Assert.Equal(240.0, Co2DerSicht(1017, 3));      // ein anderes Projekt: seine eigene Kopie
        }

        private static double Co2DerSicht(int idProjekt, int idBrennstoff)
        {
            string quelle = ProjektBrennstoffe.Sicht(idProjekt, out DbParam[] sicht);
            return Convert.ToDouble(DataRepository.ExecuteScalar("SELECT b.CO2 FROM " + quelle + " AS b WHERE b.ID = ?",
                                                                 ProjektBrennstoffe.Mit(sicht, new DbParam("@id", idBrennstoff))),
                                    CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Die Emissionsquelle des Laufs liest den Brennstoff des Projekts: der Rückfall der Ebene STAMM
        /// (Staub bei abgewählter Art) und der Gerätebrennstoff ohne Träger folgen der Kopie, nicht dem
        /// Katalog.
        /// </summary>
        [Fact]
        public void Die_Emissionsquelle_folgt_der_Kopie_nicht_dem_Katalog()
        {
            if (!_db.Vorhanden) return;
            string modus = Emissionsquelle.Modus(1030);
            Emissionsfaktoren vorherTraeger = Emissionsquelle.Fuer(1030, TRAEGER_1030, ERDGAS_E, modus);
            Emissionsfaktoren vorherGeraet = Emissionsquelle.Fuer(1030, 0, ERDGAS_E, modus);

            DataRepository.ExecuteNonQuery("UPDATE Tab_Brennstoff_Stamm SET CO2 = 999, SO2 = 999, NOx = 999, Staub = 999 WHERE ID = 3");
            Emissionsfaktoren nachTraeger = Emissionsquelle.Fuer(1030, TRAEGER_1030, ERDGAS_E, modus);
            Emissionsfaktoren nachGeraet = Emissionsquelle.Fuer(1030, 0, ERDGAS_E, modus);
            Assert.Equal(Faktoren(vorherTraeger), Faktoren(nachTraeger));
            Assert.Equal(Faktoren(vorherGeraet), Faktoren(nachGeraet));

            // Gegenprobe: Die Kopie wirkt.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Brennstoff SET CO2 = 123, Staub = 7 WHERE ID_Projekt = 1030 AND ID_Brennstoff = 3");
            Assert.Equal(123.0, Emissionsquelle.Fuer(1030, 0, ERDGAS_E, modus).Co2GKwh);
            Assert.Equal(7.0, Emissionsquelle.Fuer(1030, 0, ERDGAS_E, modus).StaubMgKwh);
        }

        private static string Faktoren(Emissionsfaktoren f) =>
            string.Join("|", f.Co2GKwh.ToString("R", CultureInfo.InvariantCulture), f.So2MgKwh.ToString("R", CultureInfo.InvariantCulture),
                        f.NoxMgKwh.ToString("R", CultureInfo.InvariantCulture), f.StaubMgKwh.ToString("R", CultureInfo.InvariantCulture));

        // =====================================================================
        //  Katalogabgleich: der Stamm ändert sich, die Kopien bleiben
        // =====================================================================

        [Fact]
        public void Der_Abgleich_aendert_den_Stamm_und_laesst_die_Projektkopien()
        {
            if (!_db.Vorhanden) return;

            // Erdgas E als Auslieferungssatz mit Schlüssel und Prüfsumme; ein Projekt mit Auslegung.
            DataRepository.ExecuteNonQuery("UPDATE Tab_Brennstoff_Stamm SET \"ReadOnly\" = 1 WHERE ID = 3");
            KatalogfassungStufe2Schema.Ausfuehren(null);
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_PufferAuslegung (ID_Projekt) VALUES (1045)");
            ProjektkopienKatalogeSchema.Saat(null);
            // Ein Projekt, das (noch) keine Kopie von Erdgas E führt: Die Vorstufe des Abgleichs legt sie an.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Brennstoff WHERE ID_Projekt = 1017 AND ID_Brennstoff = 3");

            string kopienVorher = Bild("SELECT ID_Projekt, ID_Brennstoff, " + FACHSPALTEN + " FROM Tab_Brennstoff WHERE ID_Projekt <> 1017 ORDER BY ID");
            double pufferVorher = PufferAuslegungParameter.Lesen(1045).Wert("Konstante.Wh_je_l_K");
            Emissionsfaktoren efVorher = Emissionsquelle.Fuer(1030, 0, ERDGAS_E, Emissionsquelle.Modus(1030));

            Katalogpaket paket = Katalogpaket.AusBytes(Katalogpaket.AusDatenbank(2).Bytes());
            Katalogtabelle brs = Katalogfassung.Tabelle("Tab_Brennstoff_Stamm");
            Katalogpaketsatz erdgas = paket.Tabellen.Single(t => t.Tabelle == brs.Tabelle).Saetze.Single();
            erdgas.Werte["CO2"] = 199.0;
            erdgas.Werte["Hi"] = 10.4;
            erdgas.Pruefsumme = Neusumme(brs, erdgas);
            Katalogtabelle pap = Katalogfassung.Tabelle(PufferAuslegungSchema.TAB_PARAMETER);
            Katalogpaketsatz konstante = paket.Tabellen.Single(t => t.Tabelle == pap.Tabelle).Saetze
                                              .Single(s => Equals(s.Werte["Schluessel"], "Pufferauslegung.Konstante.Wh_je_l_K"));
            konstante.Werte["Wert"] = 1.0;
            konstante.Pruefsumme = Neusumme(pap, konstante);

            KatalogabgleichErgebnis e = Katalogabgleich.Ausfuehren(paket, nurPruefen: false);
            Assert.True(e.Ausgefuehrt, e.Meldung);
            Assert.Equal(2, e.Aktualisiert);

            // Der Stamm trägt den neuen Auslieferungsstand …
            Assert.Equal(199.0, Zahl2("SELECT CO2 FROM Tab_Brennstoff_Stamm WHERE ID = 3"));
            Assert.Equal(1.0, PufferAuslegungParameter.Lesen().Wert("Konstante.Wh_je_l_K"));
            // … die Kopien bleiben, wie sie waren, und 1017 hat seine Kopie mit dem ALTEN Wert.
            Assert.Equal(kopienVorher, Bild("SELECT ID_Projekt, ID_Brennstoff, " + FACHSPALTEN + " FROM Tab_Brennstoff WHERE ID_Projekt <> 1017 ORDER BY ID"));
            Assert.Equal(240.0, Zahl2("SELECT CO2 FROM Tab_Brennstoff WHERE ID_Projekt = 1017 AND ID_Brennstoff = 3"));
            Assert.Equal(pufferVorher, PufferAuslegungParameter.Lesen(1045).Wert("Konstante.Wh_je_l_K"));
            Assert.Equal(Faktoren(efVorher), Faktoren(Emissionsquelle.Fuer(1030, 0, ERDGAS_E, Emissionsquelle.Modus(1030))));

            // „Auf Katalog zurücksetzen" holt den neuen Stand ins Projekt.
            Assert.True(ProjektBrennstoffe.Zuruecksetzen(1030, 3));
            Assert.Equal(199.0, Zahl2("SELECT CO2 FROM Tab_Brennstoff WHERE ID_Projekt = 1030 AND ID_Brennstoff = 3"));
            Assert.True(ProjektPufferparameter.Zuruecksetzen(1045) > 0);
            Assert.Equal(1.0, PufferAuslegungParameter.Lesen(1045).Wert("Konstante.Wh_je_l_K"));
        }

        /// <summary>
        /// <b>Jahressummen vor und nach einem Katalog-Update</b> auf Projekt 1030 (BHKW-Kaskade mit
        /// Erdgas) und 1017 (Wärmepumpe, Strom): Jeder Wert des Brennstoffkatalogs ändert sich, das
        /// gespeicherte Ergebnis bleibt Zahl für Zahl gleich.
        /// </summary>
        [Fact]
        public void Projekte_rechnen_nach_einem_Katalogupdate_wie_vorher()
        {
            if (!_db.Vorhanden) return;
            var vorher = new Dictionary<int, string>();
            foreach (int p in new[] { 1030, 1017 }) vorher[p] = Rechne(p);

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Brennstoff_Stamm SET Bezeichner = Bezeichner || ' neu', Hi = Hi * 1.5 + 1, Hs = Hs * 1.5 + 1, " +
                "CO2 = COALESCE(CO2, 0) * 2 + 7, SO2 = COALESCE(SO2, 0) * 2 + 7, NOx = COALESCE(NOx, 0) * 2 + 7, " +
                "Staub = COALESCE(Staub, 0) * 2 + 7, PE_Faktor = COALESCE(PE_Faktor, 0) + 1, " +
                "Standard_Grundpreis = COALESCE(Standard_Grundpreis, 0) + 50, " +
                "Standard_Arbeitspreis = COALESCE(Standard_Arbeitspreis, 0) + 0.5, " +
                "Standard_Leistungspreis = COALESCE(Standard_Leistungspreis, 0) + 5");

            foreach (int p in new[] { 1030, 1017 }) Assert.Equal(vorher[p], Rechne(p));
        }

        private static string Rechne(int idProjekt)
        {
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(idProjekt, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            var sb = new StringBuilder();
            foreach (string t in Ergebnistabellen())
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM \"" + t + "\" WHERE ID_Ergebnis IN (SELECT ID FROM Tab_Ergebnis WHERE ID_Projekt = ?) ORDER BY 1",
                    new DbParam("@p", idProjekt));
                if (dt == null) continue;
                sb.Append('#').Append(t).Append('\n');
                foreach (DataRow r in dt.Rows)
                {
                    foreach (DataColumn c in dt.Columns)
                    {
                        if (c.ColumnName == "ID" || c.ColumnName.StartsWith("ID_", StringComparison.Ordinal)) continue;
                        sb.Append(c.ColumnName).Append('=').Append(Convert.ToString(r[c], CultureInfo.InvariantCulture)).Append(';');
                    }
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        private static IEnumerable<string> Ergebnistabellen()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT m.name FROM sqlite_master m WHERE m.type = 'table' AND m.name LIKE 'Tab_Ergebnis%' " +
                "AND EXISTS (SELECT 1 FROM pragma_table_info(m.name) p WHERE p.name = 'ID_Ergebnis') ORDER BY m.name");
            foreach (DataRow r in dt.Rows) yield return Convert.ToString(r[0], CultureInfo.InvariantCulture);
        }

        // =====================================================================
        //  Projektdialog: übernehmen, bearbeiten, zurücksetzen
        // =====================================================================

        [Fact]
        public void Uebernehmen_bearbeiten_und_zuruecksetzen()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Brennstoff WHERE ID_Projekt = 1030 AND ID_Brennstoff = 5");
            Assert.Contains(ProjektBrennstoffe.OhneKopie(1030), k => k.IdBrennstoff == 5);
            Assert.DoesNotContain(ProjektBrennstoffe.Liste(1030), e => e.IdBrennstoff == 5);

            Assert.True(ProjektBrennstoffe.Uebernehmen(1030, 5));
            Assert.False(ProjektBrennstoffe.Uebernehmen(1030, 5));            // eine stehende Kopie bleibt
            Assert.DoesNotContain(ProjektBrennstoffe.OhneKopie(1030), k => k.IdBrennstoff == 5);
            ProjektBrennstoffe.Eintrag neu = ProjektBrennstoffe.Liste(1030).Single(e => e.IdBrennstoff == 5);
            Assert.False(neu.WeichtAb);
            Assert.True(neu.KatalogVorhanden);

            Assert.Null(ProjektBrennstoffe.Speichern(1030, 5, new Dictionary<string, double?> { ["CO2"] = 275.5, ["Staub"] = null }));
            ProjektBrennstoffe.Eintrag bearbeitet = ProjektBrennstoffe.Liste(1030).Single(e => e.IdBrennstoff == 5);
            Assert.Equal(275.5, bearbeitet.Werte["CO2"]);
            Assert.Null(bearbeitet.Werte["Staub"]);
            Assert.Equal(new[] { "CO2", "Staub" }, bearbeitet.Abweichungen);

            Assert.NotNull(ProjektBrennstoffe.Speichern(1030, 5, new Dictionary<string, double?> { ["Bezeichner"] = 1 }));
            Assert.NotNull(ProjektBrennstoffe.Speichern(1030, 5, new Dictionary<string, double?> { ["CO2"] = -1 }));
            Assert.NotNull(ProjektBrennstoffe.Speichern(1030, 5, new Dictionary<string, double?> { ["CO2"] = double.NaN }));
            Assert.NotNull(ProjektBrennstoffe.Speichern(1030, 999, new Dictionary<string, double?> { ["CO2"] = 1 }));

            Assert.True(ProjektBrennstoffe.Zuruecksetzen(1030, 5));
            Assert.False(ProjektBrennstoffe.Liste(1030).Single(e => e.IdBrennstoff == 5).WeichtAb);

            // Ein Projekt ohne Kopien (älteres Paket) bekommt alle Brennstoffarten auf einmal.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Brennstoff WHERE ID_Projekt = 1017");
            Assert.Equal((int)Zahl("SELECT COUNT(*) FROM Tab_Brennstoff_Stamm"), ProjektBrennstoffe.Sichern(1017));
            Assert.Equal(0, ProjektBrennstoffe.Sichern(1017));
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Brennstoff WHERE ID_Projekt = 1017");
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Assert.Equal((int)Zahl("SELECT COUNT(*) FROM Tab_Brennstoff_Stamm"), ProjektBrennstoffe.Sichern(v, 1017));
                v.Commit();
            }

            // Ohne Katalogsatz bleibt die Kopie, wie sie ist.
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Brennstoff_Stamm WHERE ID = 5");
            Assert.False(ProjektBrennstoffe.Zuruecksetzen(1030, 5));
            Assert.False(ProjektBrennstoffe.Liste(1030).Single(e => e.IdBrennstoff == 5).KatalogVorhanden);
        }

        /// <summary>
        /// <b>Die Projektanlage kopiert sofort</b> — über den Assistenten wie über den zweiten Anlageweg
        /// (<c>ProjektCtrl.Insert</c>): Das neue Projekt führt je Brennstoffart eine Kopie, wertgleich zum
        /// Katalog, und hängt nicht bis zum ersten Abgleich am Stamm.
        /// </summary>
        [Fact]
        public void Ein_neues_Projekt_bekommt_seine_Brennstoffkopien_sofort()
        {
            if (!_db.Vorhanden) return;
            var modell = new ProjektModel
            {
                m_szProjektname = "Brennstoffkopie Assistent", m_szBearbeiter = "Probe", m_szBeschreibung = "",
                m_szKunde = "", m_szKlimaregion = "",
                m_Aenderungsdatum = new DateTime(2026, 10, 3), m_Erstelldatum = new DateTime(2026, 10, 3),
            };
            int perAssistent = 0;
            Assert.True(new WizardCtrl().Add_Projekt(ref perAssistent, modell));
            var ctrl = new ProjektCtrl
            {
                m_szProjektname = "Brennstoffkopie Projektdialog", m_szBearbeiter = "Probe",
                m_Aenderungsdatum = new DateTime(2026, 10, 3), m_Erstelldatum = new DateTime(2026, 10, 3),
            };
            Assert.True(ctrl.Insert());

            string katalog = Bild("SELECT ID AS Art, " + FACHSPALTEN + " FROM Tab_Brennstoff_Stamm ORDER BY ID");
            foreach (int p in new[] { perAssistent, ctrl.m_ID })
            {
                Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Brennstoff_Stamm"),
                             Zahl("SELECT COUNT(*) FROM Tab_Brennstoff WHERE ID_Projekt = " + p));
                Assert.Equal(katalog, Bild("SELECT ID_Brennstoff AS Art, " + FACHSPALTEN + " FROM Tab_Brennstoff WHERE ID_Projekt = " + p +
                                           " ORDER BY ID_Brennstoff"));
            }
            Assert.Equal(0, ProjektBrennstoffe.OffeneKopien());
        }

        /// <summary>Ein Paket ohne Brennstoffkopien (älterer Stand) bekommt sie beim Import aus dem Katalog des Ziels.</summary>
        [Fact]
        public void Ein_Paketimport_ohne_Kopien_legt_sie_an()
        {
            if (!_db.Vorhanden) return;
            const string name = "Referenz BHKW-Kaskade (Regressionstest)";
            string ordner = Path.Combine(Path.GetTempPath(), "epos-pbrs-alt-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                // Das Paket entsteht ohne Kopien - wie aus einer Datenbank vor dem Schemaschritt.
                DataRepository.ExecuteNonQuery("DELETE FROM Tab_Brennstoff WHERE ID_Projekt = 1030");
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(name, paket));
                int neu = io.Importieren(paket, "Transfer ohne Kopien", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Brennstoff_Stamm"),
                             Zahl("SELECT COUNT(*) FROM Tab_Brennstoff WHERE ID_Projekt = " + neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        [Fact]
        public void Die_Pufferauslegung_legt_ihre_Vorgaben_beim_Speichern_als_Kopie_an()
        {
            if (!_db.Vorhanden) return;
            Assert.False(ProjektPufferparameter.HatKopie(1045));
            PufferAuslegungVorbelegung v = PufferAuslegungCtrl.Vorbelegen(1045, null);
            Assert.True(PufferAuslegungCtrl.Speichern(1045, null, v.Eingang, null) > 0);
            Assert.True(ProjektPufferparameter.HatKopie(1045));
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegungParameter_STAMM"),
                         Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegungParameter WHERE ID_Projekt = 1045"));

            DataRepository.ExecuteNonQuery("UPDATE Tab_PufferAuslegungParameter SET Wert = 2 WHERE ID_Projekt = 1045 " +
                                           "AND Schluessel = 'Pufferauslegung.Konstante.Wh_je_l_K'");
            Assert.Equal(2.0, PufferAuslegungParameter.Lesen(1045).Wert("Konstante.Wh_je_l_K"));
            Assert.NotEqual(2.0, PufferAuslegungParameter.Lesen().Wert("Konstante.Wh_je_l_K"));
            Assert.NotEqual(2.0, PufferAuslegungParameter.Lesen(1030).Wert("Konstante.Wh_je_l_K"));
            Assert.Contains(PufferAuslegungCtrl.Vorbelegen(1045, null).Herkunft,
                            h => h.Text.Contains(ProjektPufferparameter.TAB, StringComparison.Ordinal));
        }

        // =====================================================================
        //  Duplizieren und Projektpaket
        // =====================================================================

        [Fact]
        public void Duplizieren_und_Projektpaket_tragen_die_Kopien()
        {
            if (!_db.Vorhanden) return;
            const string name = "Referenz BHKW-Kaskade (Regressionstest)";
            Assert.Null(ProjektBrennstoffe.Speichern(1030, 3, new Dictionary<string, double?> { ["CO2"] = 201.25 }));
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_PufferAuslegung (ID_Projekt) VALUES (1030)");
            Assert.True(ProjektPufferparameter.Sichern(1030) > 0);
            DataRepository.ExecuteNonQuery("UPDATE Tab_PufferAuslegungParameter SET Wert = 3 WHERE ID_Projekt = 1030 " +
                                           "AND Schluessel = 'Pufferauslegung.Konstante.Wh_je_l_K'");
            string bild = Kopienbild(1030);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Brennstoffkopie");
            Assert.True(kopie > 0);
            Assert.Equal(bild, Kopienbild(kopie));
            Assert.Equal(201.25, Zahl2("SELECT CO2 FROM Tab_Brennstoff WHERE ID_Projekt = " + kopie + " AND ID_Brennstoff = 3"));

            string ordner = Path.Combine(Path.GetTempPath(), "epos-pbrs-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "p.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(name, paket));
                int neu = io.Importieren(paket, "Transfer Brennstoffe", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.Equal(bild, Kopienbild(neu));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }

            // Mit dem Projekt gehen seine Kopien.
            Assert.True(new ProjektCtrl().Delete(kopie));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Brennstoff WHERE ID_Projekt = " + kopie));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_PufferAuslegungParameter WHERE ID_Projekt = " + kopie));
        }

        private static string Kopienbild(int idProjekt) =>
            Bild("SELECT ID_Brennstoff, " + FACHSPALTEN + " FROM Tab_Brennstoff WHERE ID_Projekt = " + idProjekt + " ORDER BY ID_Brennstoff") +
            Bild("SELECT Schluessel, Wert, Einheit, Quelle, Herkunftsart FROM Tab_PufferAuslegungParameter WHERE ID_Projekt = " +
                 idProjekt + " ORDER BY Schluessel");

        // =====================================================================
        //  Konditionierungsvorlagen: Übernehmen ist kopieren
        // =====================================================================

        /// <summary>
        /// Die Konditionierungsvorlagen brauchen keine eigene Kopietabelle: Kein Projektgebäude und keine
        /// Zone verweist über eine ID auf eine Vorlage, der Lauf liest nur die Projektzeilen
        /// (<c>ID_Gebaeude</c>), und ein Abgleich, der eine Vorlage ändert, lässt sie stehen.
        /// </summary>
        [Fact]
        public void Konditionierungsvorlagen_werden_bei_der_Uebernahme_kopiert()
        {
            if (!_db.Vorhanden) return;
            foreach (string t in new[] { "Tab_Gebaeude", "Tab_Zone", "Tab_Projekt" })
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM pragma_table_info('" + t + "') WHERE name LIKE '%Vorlage%'"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IS NOT NULL AND ID_Vorlage IS NOT NULL"));

            string projekt = Bild("SELECT * FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IS NOT NULL ORDER BY ID") +
                             Bild("SELECT * FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude IS NOT NULL ORDER BY ID");
            DataRepository.ExecuteNonQuery("UPDATE Tab_Konditionierungsvorgabe SET Wert = COALESCE(Wert, 0) + 1 WHERE ID_Vorlage IS NOT NULL");
            DataRepository.ExecuteNonQuery("UPDATE Tab_Konditionierungskalender SET Bemerkung = 'geändert' WHERE ID_Vorlage IS NOT NULL");
            Assert.Equal(projekt, Bild("SELECT * FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IS NOT NULL ORDER BY ID") +
                                  Bild("SELECT * FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude IS NOT NULL ORDER BY ID"));
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        private static string Neusumme(Katalogtabelle t, Katalogpaketsatz s) =>
            Katalogfassung.Pruefsumme(t, s.Werte, s.Kinder.ToDictionary(k => k.Key,
                k => (IReadOnlyList<IReadOnlyDictionary<string, object>>)k.Value.Cast<IReadOnlyDictionary<string, object>>().ToList()));

        private static long Zahl(string sql) => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static double Zahl2(string sql) => Convert.ToDouble(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(string sql) => Convert.ToString(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Bild(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
                sb.Append(string.Join("|", r.ItemArray.Select(x => x == DBNull.Value ? "∅" : Convert.ToString(x, CultureInfo.InvariantCulture))))
                  .Append('\n');
            using (SHA256 sha = SHA256.Create())
                return dt.Rows.Count.ToString(CultureInfo.InvariantCulture) + ":" +
                       Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
        }
    }
}
