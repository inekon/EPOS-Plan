using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt der <b>Katalogsätze der Klassen M und A</b> (Entscheid E51, Konzept-Nachtrag
    /// N1.58; Nummer bei <see cref="GebaeudeSaatSchema"/>).
    ///
    /// <para><b>Geprüft wird:</b> die Nummer; die sechs Sätze samt Herleitung aus den Rohwerten der
    /// Quellen (GEG Anlage 1, Stein/Loga 2025 Tab. 26 bis 28, 33 und 34) — Flächen, flächengewichtete
    /// U-Werte, Längen und ψ; der Stand der Testdatenbank (sechs Sätze, <c>ReadOnly</c>, Klassen, EH55);
    /// dass der Schritt nur anlegt, was unter seinem Namen fehlt, nie überschreibt und wiederholbar ist;
    /// Repo-Datei, Werkzeug, Migration und Testkopie führen ihn nach den Baualtersklassen.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — ein Fall schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeSaatSchemaTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        /// <summary>Die Nummer folgt lückenlos auf die Baualtersklassen; der Zielstand reicht bis zu ihr.</summary>
        [Fact]
        public void Die_Nummer_folgt_auf_die_Baualtersklassen_und_ist_das_Ziel()
        {
            Assert.Equal(BaualtersklassenSchema.SCHRITT + 1, GebaeudeSaatSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= GebaeudeSaatSchema.SCHRITT,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter " + GebaeudeSaatSchema.SCHRITT + ".");
        }

        /// <summary>
        /// Sechs Sätze mit eindeutigen, neutralen Namen ohne Kennzahl; drei für M, drei für A; nur
        /// „EFH-GEG-EH55" trägt einen Energiestandard (EH55); jede Beschreibung nennt die Quelle.
        /// </summary>
        [Fact]
        public void Sechs_Saetze_drei_fuer_M_drei_fuer_A_und_einer_mit_EH55()
        {
            IReadOnlyList<GebaeudeSaat> s = GebaeudeSaatSchema.Saat;
            Assert.Equal(new[] { "EFH-GEG-Ref", "EFH-GEG-EH55", "KMH-GEG-typ", "EFH-bis1859-U", "KMH-bis1859-U", "EFH-bis1859-TS" },
                         s.Select(x => x.Bezeichner));
            Assert.Equal("MMMAAA", string.Concat(s.Select(x => x.Klasse)));
            Assert.Equal(new[] { Energiestandard.EH55 }, s.Where(x => x.Energiestandard != null).Select(x => x.Energiestandard));
            Assert.Equal("EFH-GEG-EH55", s.Single(x => x.Energiestandard != null).Bezeichner);
            Assert.Contains(Energiestandard.EH55, Energiestandard.CODES);
            foreach (GebaeudeSaat x in s)
            {
                Assert.Contains("CC BY 4.0", x.Beschreibung, StringComparison.Ordinal);
                Assert.Contains("Stein, B.; Loga, T. (2025)", x.Beschreibung, StringComparison.Ordinal);
                Assert.Contains(x.Gebaeudeart, new[] { GebaeudeSaattabelle.EFH, GebaeudeSaattabelle.KMH });
                // Die Regeln des Musters: Bewohner, innere Gewinne, Bauweise, Fenster je Richtung.
                Assert.Equal(x.Wohnflaeche / 35.0, x.Bewohner, 9);
                Assert.Equal(5.0 * x.Wohnflaeche, x.InnereGewinne, 6);
                Assert.Equal(50.0 * x.Wohnflaeche, x.Bauweise, 6);
                Assert.Equal(x.FlaecheFenster, x.FensterSued + x.FensterOstWest + x.FensterNord, 9);
                Assert.Equal(x.FensterSued, x.FensterNord);
                Assert.Equal(2.0 * x.FensterSued, x.FensterOstWest, 9);
                Assert.Equal(x.Klasse == "M" ? 0.6 : 0.7, x.Luftwechsel);
            }
        }

        /// <summary>Die Rohwerte eines Satzes aus der Quelle — neun Flächen, zehn U-Werte samt g, ΔU_WB.</summary>
        private sealed record Roh(
            double Dach, double Ogd, double Aw, double WandKeller, double WandErde, double Kd, double Boden, double Fe, double Tuer,
            double UDach, double UOgd, double UAw, double UWandKeller, double UWandErde, double UKd, double UBoden,
            double UFe, double G, double UTuer, double DeltaU);

        /// <summary>
        /// Die Rohwerte je Satz: Flächen aus Stein/Loga Tab. 27 (S. 67), U-Werte aus GEG Anlage 1 (M1,
        /// M2 = M1 × 0,70) bzw. Tab. 28 (S. 68) und Tab. 33 (S. 73), ΔU_WB aus GEG Nr. 2 bzw. Tab. 34 (S. 74).
        /// </summary>
        private static readonly Dictionary<string, Roh> Rohwerte = new Dictionary<string, Roh>(StringComparer.Ordinal)
        {
            ["EFH-GEG-Ref"] = new Roh(113.9, 35.8, 193.3, 2.7, 17.7, 15.1, 100.1, 30.6, 3.4,
                                      0.20, 0.20, 0.28, 0.35, 0.35, 0.35, 0.35, 1.3, 0.60, 1.8, 0.05),
            ["EFH-GEG-EH55"] = new Roh(113.9, 35.8, 193.3, 2.7, 17.7, 15.1, 100.1, 30.6, 3.4,
                                       0.14, 0.14, 0.196, 0.245, 0.245, 0.245, 0.245, 0.91, 0.60, 1.26, 0.035),
            ["KMH-GEG-typ"] = new Roh(194.1, 27.7, 326.4, 4.1, 16.1, 61.9, 104.7, 67.1, 5.5,
                                      0.132, 0.132, 0.158, 0.158, 0.158, 0.158, 0.158, 0.951, 0.55, 0.951, 0.031),
            ["EFH-bis1859-U"] = new Roh(69.9, 55.0, 227.8, 1.9, 11.5, 54.9, 48.5, 27.0, 3.2,
                                        1.318, 1.100, 1.372, 1.372, 1.372, 1.015, 1.015, 3.492, 0.594, 3.492, 0.016),
            ["KMH-bis1859-U"] = new Roh(130.9, 52.5, 310.0, 2.6, 15.0, 88.1, 48.1, 55.2, 4.8,
                                        1.318, 1.100, 1.372, 1.372, 1.372, 1.015, 1.015, 3.545, 0.594, 3.545, 0.016),
            ["EFH-bis1859-TS"] = new Roh(69.9, 55.0, 227.8, 1.9, 11.5, 54.9, 48.5, 27.0, 3.2,
                                         0.708, 0.470, 1.082, 0.790, 1.082, 0.704, 0.908, 2.075, 0.594, 2.075, 0.040),
        };

        /// <summary>
        /// DIE HERLEITUNG (Regeln im Kopf von <c>GebaeudeSaat.cs</c>): Dach = Dach + oberste
        /// Geschossdecke, Grund = Wände gegen Keller und Erdreich + Kellerdecke + Boden, je mit
        /// flächengewichtetem U; l_FW = Umfang/Fläche des Standardfensters 1,23 × 1,48 m × Fensterfläche;
        /// l_WD = l_AK = Umfang des Quadrats über Kellerdecke + Boden; ψ im Verhältnis 0,05 : 0,16 : 0,24
        /// mit Σ ψ·l = ΔU_WB × Hüllfläche. Die Sätze tragen die Werte auf ihre Stellen gerundet.
        /// </summary>
        [Fact]
        public void Die_Werte_folgen_aus_den_Rohwerten_der_Quellen()
        {
            double jeM2Fenster = 2.0 * (1.23 + 1.48) / (1.23 * 1.48);
            Assert.Equal(2.98, jeM2Fenster, 2);
            foreach (GebaeudeSaat s in GebaeudeSaatSchema.Saat)
            {
                Roh r = Rohwerte[s.Bezeichner];
                string n = s.Bezeichner + ": ";

                Nah(r.Aw, s.FlaecheAussenwand, 1e-9, n + "Außenwand");
                Nah(r.Fe, s.FlaecheFenster, 1e-9, n + "Fenster");
                Nah(r.Tuer, s.FlaecheSonstige, 1e-9, n + "Türen");
                Nah(r.Dach + r.Ogd, s.FlaecheDach, 0.051, n + "Dach");
                Nah(r.WandKeller + r.WandErde + r.Kd + r.Boden, s.FlaecheGrund, 0.051, n + "Grund");

                Nah(r.UAw, s.UAussenwand, 0.0006, n + "U Außenwand");
                Nah(r.UFe, s.UFenster, 0.0006, n + "U Fenster");
                Nah(r.UTuer, s.USonstige, 0.0006, n + "U Türen");
                Nah(r.G, s.GWert, 1e-9, n + "g");
                Nah((r.Dach * r.UDach + r.Ogd * r.UOgd) / (r.Dach + r.Ogd), s.UDach, 0.0006, n + "U Dach");
                Nah((r.WandKeller * r.UWandKeller + r.WandErde * r.UWandErde + r.Kd * r.UKd + r.Boden * r.UBoden)
                    / (r.WandKeller + r.WandErde + r.Kd + r.Boden), s.UGrund, 0.0006, n + "U Grund");
                Nah(r.DeltaU, s.DeltaUWb, 1e-9, n + "ΔU_WB");

                Nah(jeM2Fenster * r.Fe, s.LaengeFensterWand, 0.051, n + "l_FW");
                double umfang = 4.0 * Math.Sqrt(r.Kd + r.Boden);
                Nah(umfang, s.LaengeWandDach, 0.051, n + "l_WD");
                Nah(umfang, s.LaengeAussenwandKeller, 0.051, n + "l_AK");

                double k = r.DeltaU * s.Huellflaeche
                           / (0.05 * s.LaengeFensterWand + 0.16 * s.LaengeWandDach + 0.24 * s.LaengeAussenwandKeller);
                Nah(0.05 * k, s.PsiFensterWand, 0.0006, n + "ψ_FW");
                Nah(0.16 * k, s.PsiWandDach, 0.0006, n + "ψ_WD");
                Nah(0.24 * k, s.PsiAussenwandKeller, 0.0006, n + "ψ_AK");
                double summe = s.PsiFensterWand * s.LaengeFensterWand + s.PsiWandDach * s.LaengeWandDach
                               + s.PsiAussenwandKeller * s.LaengeAussenwandKeller;
                Assert.True(Math.Abs(summe - r.DeltaU * s.Huellflaeche) <= 0.02 * r.DeltaU * s.Huellflaeche,
                            n + "Σψ·l " + summe.ToString("R", CultureInfo.InvariantCulture) + " gegen ΔU_WB·A "
                            + (r.DeltaU * s.Huellflaeche).ToString("R", CultureInfo.InvariantCulture));
            }

            // M2 ist M1 × 0,70 (Effizienzhaus 55, benannte Annahme).
            GebaeudeSaat m1 = GebaeudeSaatSchema.Saat[0], m2 = GebaeudeSaatSchema.Saat[1];
            foreach (Func<GebaeudeSaat, double> u in new Func<GebaeudeSaat, double>[]
                     { x => x.UAussenwand, x => x.UFenster, x => x.UDach, x => x.UGrund, x => x.USonstige, x => x.DeltaUWb })
                Nah(0.70 * u(m1), u(m2), 1e-9, "EH55 = 0,70 × Referenz");
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank
        // =============================================================================

        /// <summary>
        /// Die Testdatenbank trägt die sechs Sätze: je einmal unter ihrem Namen, <c>ReadOnly = 1</c>,
        /// Klasse und Standard wie in der Saat, jede gesäte Spalte mit dem Wert der Saat.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_traegt_die_sechs_Saetze()
        {
            if (!_db.Vorhanden) return;

            Assert.True(GebaeudeSaatSchema.Vollstaendig());
            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= GebaeudeSaatSchema.SCHRITT);
            foreach (GebaeudeSaat s in GebaeudeSaatSchema.Saat)
            {
                DataRow r = Zeile(s.Bezeichner);
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Klasse, Convert.ToString(r["Baualtersklasse"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Energiestandard, r["Energiestandard"] == DBNull.Value ? null : Convert.ToString(r["Energiestandard"], CultureInfo.InvariantCulture));
                Assert.Equal(GebaeudeSaattabelle.TYP_WOHNGEBAEUDE, Convert.ToString(r["Typ"], CultureInfo.InvariantCulture));
                Assert.Equal(DBNull.Value, r["Baujahr"]);
                Assert.Equal(DBNull.Value, r["spez_Waermeverbrauch"]);
                Assert.Equal(DBNull.Value, r["Gebaeude_Modell"]);
                Assert.Equal(0L, Convert.ToInt64(r["Heizkreis_Aktiv"], CultureInfo.InvariantCulture));
                Wert(r, "k_Wert_Außenwand", s.UAussenwand);
                Wert(r, "k_Wert_Fenster", s.UFenster);
                Wert(r, "k_Wert_Dachflaeche", s.UDach);
                Wert(r, "k_Wert_Grundflaeche", s.UGrund);
                Wert(r, "k_Wert_Sonstiges", s.USonstige);
                Wert(r, "Fensterdurchlassgrad", s.GWert);
                Wert(r, "WBVK_Anschluß_Fenster_Wand", s.PsiFensterWand);
                Wert(r, "WBVK_Anschluß_Wand_Dach", s.PsiWandDach);
                Wert(r, "WBVK_Anschluß_Außenwand_Kellerdecke", s.PsiAussenwandKeller);
                Wert(r, "Abmessung_Anschluß_Fenster_Wand", s.LaengeFensterWand);
                Wert(r, "Abmessung_Anschluß_Wand_Dach", s.LaengeWandDach);
                Wert(r, "Abmessung_Anschluß_Außenwand_Kellerdecke", s.LaengeAussenwandKeller);
                Wert(r, "Flaeche_Außenwand", s.FlaecheAussenwand);
                Wert(r, "gesamte_Fensterflaeche", s.FlaecheFenster);
                Wert(r, "Dachflaeche", s.FlaecheDach);
                Wert(r, "Grundflaeche", s.FlaecheGrund);
                Wert(r, "Sonstige_Flaechen", s.FlaecheSonstige);
                Wert(r, "Wohnflaeche_gesamt", s.Wohnflaeche);
                Wert(r, "Nutzflaeche", s.Wohnflaeche);
                Wert(r, "Bewohner", s.Bewohner);
                Wert(r, "Flaeche_Nutzer", 35.0);
                Wert(r, "Interne_Waermegewinne", s.InnereGewinne);
                Wert(r, "Bauweise", s.Bauweise);
                Wert(r, "Raumhoehe", s.Raumhoehe);
                Wert(r, "Luftwechselrate", s.Luftwechsel);
                Wert(r, "Raumsolltemperatur_Tag", 20.0);
                Wert(r, "Raumsolltemperatur_Nachtabsenkung", 18.0);
                Wert(r, "Maximaleraumtemperatur", 24.0);
                Wert(r, "Ferienbeginn_1", 366.0);
                Wert(r, "WW_Bedarf", 700.0);
            }
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Baualtersklasse = 'M'"));
            Assert.Equal(3L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Baualtersklasse = 'A'"));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE Energiestandard = 'EH55'"));
        }

        /// <summary>
        /// NUR WAS FEHLT, NIE ÜBERSCHREIBEN, WIEDERHOLBAR: Ein gelöschter Satz kommt wieder, ein
        /// geänderter bleibt geändert, ein gleichnamiger eigener Satz des Anwenders bleibt stehen und
        /// steht im Protokoll; ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void Der_Schritt_legt_nur_an_was_fehlt_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            long vorher = Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM");
            GebaeudeSaat[] s = GebaeudeSaatSchema.Saat.ToArray();
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", new DbParam("?", s[0].Bezeichner)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude_STAMM SET k_Wert_Fenster = 9.9 WHERE Bezeichner = ?", new DbParam("?", s[3].Bezeichner)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude_STAMM SET ReadOnly = 0, Beschreibung = 'eigen' WHERE Bezeichner = ?",
                                                  new DbParam("?", s[5].Bezeichner)));
            Assert.False(GebaeudeSaatSchema.Vollstaendig());

            var bericht = new List<string>();
            GebaeudeSaatSchema.Bericht b = GebaeudeSaatSchema.Ausfuehren(bericht);
            Assert.Equal(1, b.Gesaet);
            Assert.Equal(5, b.Vorhanden.Count);
            Assert.Equal(new[] { s[5].Bezeichner }, b.Eigene);
            Assert.Contains(bericht, z => z.Contains("\"" + s[5].Bezeichner + "\"", StringComparison.Ordinal));
            Assert.True(GebaeudeSaatSchema.Vollstaendig());
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));
            Wert(Zeile(s[0].Bezeichner), "k_Wert_Außenwand", s[0].UAussenwand);
            Wert(Zeile(s[3].Bezeichner), "k_Wert_Fenster", 9.9);                                  // nie überschrieben
            Assert.Equal("eigen", Convert.ToString(Zeile(s[5].Bezeichner)["Beschreibung"], CultureInfo.InvariantCulture));

            GebaeudeSaatSchema.Bericht b2 = GebaeudeSaatSchema.Ausfuehren(null);
            Assert.Equal(0, b2.Gesaet);
            Assert.Equal(6, b2.Vorhanden.Count);
            Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM"));
        }

        /// <summary>Ohne einen einzigen Satz legt der Schritt alle sechs an — die Lage einer Anwenderdatenbank vor 149.</summary>
        [Fact]
        public void Aus_dem_Stand_davor_legt_der_Schritt_alle_sechs_an()
        {
            if (!_db.Vorhanden) return;

            foreach (GebaeudeSaat s in GebaeudeSaatSchema.Saat)
                Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", new DbParam("?", s.Bezeichner)));
            Assert.False(GebaeudeSaatSchema.Vollstaendig());

            GebaeudeSaatSchema.Bericht b = GebaeudeSaatSchema.Ausfuehren(null);
            Assert.Equal(6, b.Gesaet);
            Assert.Empty(b.Vorhanden);
            Assert.True(GebaeudeSaatSchema.Vollstaendig());
            Assert.Equal(6L, Zahl("SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ReadOnly = 1 AND Baualtersklasse IN ('A','M')"));
        }

        // =============================================================================
        //  Teil 3 - Repo-Datei, Werkzeug und Migration
        // =============================================================================

        /// <summary>
        /// <b>Die Werkzeug-Wache.</b> Migration der Schale, Werkzeug <c>Testdatenbankschema</c> und
        /// Testkopie führen den Schritt aus derselben Quelle NACH den Baualtersklassen; die Repo-Datei
        /// trägt ihn (lesend geprüft, ohne Spuren).
        /// </summary>
        [Fact]
        public void Repo_Datei_Werkzeug_und_Migration_fuehren_den_Schritt()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;

            string werkzeug = File.ReadAllText(Path.Combine(wurzel, "Werkzeuge", "Testdatenbankschema", "Program.cs"));
            int wSaat = werkzeug.IndexOf("GebaeudeSaatSchema.Ausfuehren(", StringComparison.Ordinal);
            Assert.True(wSaat > werkzeug.IndexOf("BaualtersklassenSchema.Ausfuehren(", StringComparison.Ordinal),
                        "Die Saat steht im Werkzeug nicht hinter den Baualtersklassen.");

            string migration = File.ReadAllText(Path.Combine(wurzel, "WindowsFormsApplication1", "Allgemein",
                                                             "Update", "SchemaMigration.cs"));
            Assert.Contains("SCHRITT_GEBAEUDESAAT = GebaeudeSaatSchema.SCHRITT", migration);
            int ortVorher = migration.IndexOf("new Schritt(SCHRITT_BAUALTERSKLASSEN", StringComparison.Ordinal);
            int ortSaat = migration.IndexOf("new Schritt(SCHRITT_GEBAEUDESAAT", StringComparison.Ordinal);
            Assert.True(ortVorher > 0 && ortSaat > ortVorher, "Der Schritt steht nicht hinter 148.");
            Assert.Contains("GebaeudeSaatSchema.Ausfuehren(zeilen)", migration);

            string vorrichtung = File.ReadAllText(Path.Combine(wurzel, "EPOS.Kern.Tests", "TestDatenbank.cs"));
            int vSaat = vorrichtung.IndexOf("GebaeudeSaatSchema.Ausfuehren(null)", StringComparison.Ordinal);
            Assert.True(vSaat > vorrichtung.IndexOf("BaualtersklassenSchema.Ausfuehren(null)", StringComparison.Ordinal),
                        "Die Saat steht in der Testkopie nicht hinter den Baualtersklassen.");

            string pfad = Path.Combine(wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(pfad)) return;
            LfsZeigerProbe.Sicherstellen(pfad);

            string uri = "file:" + pfad.Replace('\\', '/').Replace("?", "%3f") + "?mode=ro&immutable=1";
            using var verbindung = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = uri }.ToString());
            verbindung.Open();
            using SqliteCommand cmd = verbindung.CreateCommand();
            cmd.CommandText = "SELECT SchemaVersion FROM Tab_Applikation";
            Assert.True(Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) >= GebaeudeSaatSchema.SCHRITT);
            foreach (GebaeudeSaat s in GebaeudeSaatSchema.Saat)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Tab_Gebaeude_STAMM WHERE ReadOnly = 1 AND Bezeichner = '" + s.Bezeichner + "'";
                Assert.Equal(1L, Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture));
            }
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private static void Nah(double erwartet, double ist, double toleranz, string was)
            => Assert.True(Math.Abs(erwartet - ist) <= toleranz,
                           was + ": erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist "
                           + ist.ToString("R", CultureInfo.InvariantCulture));

        private static void Wert(DataRow r, string spalte, double erwartet)
        {
            Assert.NotEqual(DBNull.Value, r[spalte]);
            Nah(erwartet, Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture), 1e-9, spalte);
        }

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string bezeichner)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Gebaeude_STAMM WHERE Bezeichner = ?", new DbParam("?", bezeichner));
            Assert.True(dt != null && dt.Rows.Count == 1, "Kein eindeutiger Satz: " + bezeichner);
            return dt.Rows[0];
        }

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
