using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>KD-4 — Rohwerte des Klimadatenimports</b>: der Schemaschritt <see cref="KlimaRohwerteSchema"/> auf
    /// einer frisch migrierten Kopie der Testdatenbank (Tabelle, Spalten, Prüfklauseln, Kaskade, Eindeutigkeit), die Leser
    /// beider Quellen mit allen Feldern, der Schreibweg mit und ohne Rohtabelle, die Projektkopie ohne Rohwerte, die
    /// Bezeichnung auf dem Ortsweg und der Parameterbestand der Vorschau.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KlimaRohwerteTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose()
        {
            _db.Dispose();
            _kultur.Dispose();
        }

        // =====================================================================
        //  Schemaschritt
        // =====================================================================

        [Fact]
        public void Nummer_Kette_und_Register()
        {
            // KETTE: 238 haengt ueber + 1 an 237 (BauteilStrahlungSchema, DA-8).
            Assert.Equal(238, KlimaRohwerteSchema.SCHRITT);
            Assert.Equal(BauteilStrahlungSchema.SCHRITT + 1, KlimaRohwerteSchema.SCHRITT);
            Assert.True(SchemaStand.Zielversion >= KlimaRohwerteSchema.SCHRITT);
            Assert.Contains(Paketanhebung.Stufen, s => s.Nr == KlimaRohwerteSchema.SCHRITT && s.Wirkung == Paketanhebung.Art.Ddl);
            Assert.Equal(14, KlimaRohwerteSchema.ROHSPALTEN.Count);
            Assert.Equal(8 + 2 + 8, KlimaRohwerteSchema.Spalten().Count);
            Assert.True(Katalogfassung.Ausgenommen.ContainsKey(KlimaRohwerteSchema.TAB_ROH));
            KatalogDefinition k = KatalogRegistry.Finde("KLIMAREGION");
            foreach (var s in KlimaRohwerteSchema.REGIONSSPALTEN.Concat(KlimaRohwerteSchema.KATALOGSPALTEN))
                Assert.Contains(s.Spalte, k.AusschlussSpalten);
            Assert.DoesNotContain(k.Datenbloecke, b => b.Tabelle == KlimaRohwerteSchema.TAB_ROH);
        }

        [Fact]
        public void Tabelle_und_Spalten_stehen_leer_und_ohne_Nachladen()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KlimaRohwerteSchema.SchemaVollstaendig());
            string ddl = Ddl(KlimaRohwerteSchema.TAB_ROH);
            Assert.EndsWith("STRICT", ddl.Trim());
            Assert.Contains("ON DELETE CASCADE", ddl);
            Assert.Contains("PRIMARY KEY (\"ID_Klimaregion\", \"Stunde\")", ddl);
            foreach (string sp in KlimaRohwerteSchema.ROHSPALTEN)
                Assert.True(DataRepository.SpalteVorhanden(KlimaRohwerteSchema.TAB_ROH, sp), sp);

            // KD-Q7: kein Nachladen - die Tabelle ist leer, jede Bestandsregion traegt Rohdaten = 0 und keine Herkunft.
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM"));
            Assert.True(Zahl("SELECT COUNT(*) FROM Tab_Klimaregion_STAMM") > 0);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Klimaregion_STAMM WHERE Rohdaten <> 0 OR Zeitbezug IS NOT NULL " +
                                  "OR Bezugspunkt_Art IS NOT NULL OR Ausfuehrung IS NOT NULL"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Klimaregion WHERE Bezugspunkt_Art IS NOT NULL"));
            Assert.False(DataRepository.TabelleVorhanden("Tab_Klimastunde"));
            Assert.False(DataRepository.SpalteVorhanden("Tab_Klimaregion", KlimaRohwerteSchema.SPALTE_ROHDATEN));

            // Wiederholbar: ein zweiter Lauf legt nichts an.
            Assert.Equal(0, KlimaRohwerteSchema.Ausfuehren(null));
        }

        [Fact]
        public void Pruefklauseln_Eindeutigkeit_und_Kaskade()
        {
            if (!_db.Vorhanden) return;
            long id = Zahl("SELECT MIN(ID_Klimaregion) FROM Tab_Klimaregion_STAMM");
            string ins = "INSERT INTO Tab_Klimastunde_STAMM (ID_Klimaregion, Stunde, Windrichtung_Grad) VALUES (" + id + ", ";
            Ausfuehren(ins + "1, 90)");
            Ausfuehren(ins + "8760, NULL)");
            Assert.ThrowsAny<Exception>(() => Ausfuehren(ins + "1, 10)"));          // (Region, Stunde) eindeutig
            Assert.ThrowsAny<Exception>(() => Ausfuehren(ins + "0, 10)"));
            Assert.ThrowsAny<Exception>(() => Ausfuehren(ins + "8761, 10)"));
            Assert.ThrowsAny<Exception>(() => Ausfuehren(ins + "5, 361)"));
            Assert.ThrowsAny<Exception>(() => Ausfuehren(ins + "5, 'Nord')"));      // STRICT
            Assert.ThrowsAny<Exception>(() => Ausfuehren(
                "INSERT INTO Tab_Klimastunde_STAMM (ID_Klimaregion, Stunde) VALUES (987654, 1)"));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Klimaregion_STAMM SET Rohdaten = 2 WHERE ID_Klimaregion = " + id));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Klimaregion_STAMM SET Zeitbezug = 'MESZ' WHERE ID_Klimaregion = " + id));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Klimaregion_STAMM SET Bezugspunkt_Art = 'frei' WHERE ID_Klimaregion = " + id));
            Assert.ThrowsAny<Exception>(() => Ausfuehren("UPDATE Tab_Klimaregion SET Entfernung_km = -1"));
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM WHERE ID_Klimaregion = " + id));

            Ausfuehren("DELETE FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + id);
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM"));
        }

        // =====================================================================
        //  Leser
        // =====================================================================

        [Fact]
        public void TRY_Leser_liefert_alle_Felder_im_Ortszeitraster()
        {
            var zeilen = new List<string>
            {
                "Probe - erfundene Werte",
                "***",
                "3929310 2478193  1  1  2  -4.5 1007  40  2.4  5  3.1  82   11   22  251  -291  2",
                "3929310 2478193  1  1  1  -5.5 1006  30  3.4  3  3.2  81    0    0  250  -290  1",
            };
            List<TmyHourlyData> stunden = DwdTryLeser.Lesen(zeilen, out TryKopf kopf, vollesJahr: false);
            Assert.Equal(2, stunden.Count);
            Assert.Equal(KlimaRohwerteSchema.ZEITBEZUG_MEZ, kopf.Rohwerte.Zeitbezug);
            Assert.Equal(new[] { 1, 2 }, kopf.Rohwerte.Stunden.Select(s => s.Stunde));

            KlimaRohstunde r = kopf.Rohwerte.Stunden[1];
            Assert.Equal(-4.5, r.Temperatur);
            Assert.Equal(1007.0, r.Luftdruck_hPa);
            Assert.Equal(40.0, r.Windrichtung_Grad);
            Assert.Equal(2.4, r.Windgeschwindigkeit_m_s);
            Assert.Equal(5.0, r.Bedeckungsgrad_Achtel);
            Assert.Equal(3.1, r.Wasserdampfgehalt_g_kg);
            Assert.Equal(82.0, r.Luftfeuchte_Prozent);
            Assert.Equal(11.0, r.DirektHorizontal);
            Assert.Equal(22.0, r.DiffusHorizontal);
            Assert.Equal(251.0, r.Gegenstrahlung);
            Assert.Equal(-291.0, r.Ausstrahlung);
            Assert.Equal(2, r.QualitaetStrahlung);
            Assert.Null(r.GlobalHorizontal);          // TRY fuehrt sie nicht (B + D ist gerechnet)
            Assert.Null(r.DirektNormal);

            // Die Rechenreihe bleibt, wie sie war: Wind 0, Global B + D.
            Assert.All(stunden, s => Assert.Equal(0.0, s.WindSpeed));
            Assert.Equal(33.0, stunden.Single(s => s.Temperature == -4.5).GlobalIrradiance);
        }

        [Fact]
        public void PVGIS_Leser_liefert_Windrichtung_und_Luftdruck()
        {
            string json = File.ReadAllText(Probe("pvgis_tmy_stuttgart_72h.json"));
            PvgisAbruf abruf = PVGIS_EPW_Downloader.AusJsonAbruf(json);
            Assert.All(abruf.Stunden, s => Assert.Equal(180.0, s.Windrichtung));
            Assert.All(abruf.Stunden, s => Assert.Equal(101300.0, s.Luftdruck_Pa));

            KlimaRohreihe r = KlimaRohreihe.AusPvgis(abruf.Stunden);
            Assert.Equal(KlimaRohwerteSchema.ZEITBEZUG_UTC, r.Zeitbezug);
            Assert.Equal(Enumerable.Range(1, 72), r.Stunden.Select(s => s.Stunde));
            KlimaRohstunde e = r.Stunden[0];
            TmyHourlyData q = abruf.Stunden[0];
            Assert.Equal(1013.0, e.Luftdruck_hPa);
            Assert.Equal(180.0, e.Windrichtung_Grad);
            Assert.Equal(q.Temperature, e.Temperatur);
            Assert.Equal(q.Humidity, e.Luftfeuchte_Prozent);
            Assert.Equal(q.GlobalIrradiance, e.GlobalHorizontal);
            Assert.Equal(q.DirectIrradiance, e.DirektNormal);
            Assert.Equal(q.DiffuseIrradiance, e.DiffusHorizontal);
            Assert.Equal(q.Gegenstrahlung, e.Gegenstrahlung);
            Assert.Equal(q.WindSpeed, e.Windgeschwindigkeit_m_s);
            Assert.Null(e.Bedeckungsgrad_Achtel);
            Assert.Null(e.Wasserdampfgehalt_g_kg);
            Assert.Null(e.Ausstrahlung);
            Assert.Null(e.QualitaetStrahlung);
            Assert.Null(e.DirektHorizontal);
        }

        // =====================================================================
        //  Schreibweg, Projektkopie
        // =====================================================================

        [Fact]
        public async Task Import_fuellt_Rohtabelle_und_laesst_die_Rechenspalten_wie_ohne()
        {
            if (!_db.Vorhanden) return;
            string ordner = Path.Combine(Path.GetTempPath(), "epos-kd4-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string datei = Path.Combine(ordner, "TRY_kd4_probe.dat");
                File.WriteAllText(datei, Jahresreihe(), Encoding.ASCII);

                KlimaImportErgebnis mit = await KlimaImportAblauf.Laufen(Dateiauftrag("KD4 mit Rohwerten", datei), null);
                Assert.True(mit.Erfolgreich, mit.Meldung);
                Assert.Equal(8760, mit.Rohstunden);
                Assert.Equal(8760L, Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM WHERE ID_Klimaregion = " + mit.Id));
                Assert.Equal(1L, Zahl("SELECT Rohdaten FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + mit.Id));
                Assert.Equal("MEZ", Text("SELECT Zeitbezug FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + mit.Id));
                Assert.Equal("datei", Text("SELECT Bezugspunkt_Art FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + mit.Id));
                Assert.Equal("TRY_kd4_probe.dat", Text("SELECT Ausfuehrung FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + mit.Id));
                Assert.Equal(300.0, Reell("SELECT Bezugspunkt_Hoehe_m FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + mit.Id));
                Assert.Contains(WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_ROHWERTE_GESPEICHERT,
                                Text("SELECT Details FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + mit.Id));

                // Stunde 1 = 1. Januar, 1 Uhr MEZ - ungedreht, im Raster der Datei.
                DataTable h1 = DataRepository.GetDataTable(
                    "SELECT * FROM Tab_Klimastunde_STAMM WHERE ID_Klimaregion = ? AND Stunde = 1", new DbParam("@id", mit.Id));
                DataRow z = h1.Rows[0];
                Assert.Equal(5.0, Convert.ToDouble(z["Temperatur_C"], CultureInfo.InvariantCulture));
                Assert.Equal(1013.0, Convert.ToDouble(z["Luftdruck_hPa"], CultureInfo.InvariantCulture));
                Assert.Equal(180.0, Convert.ToDouble(z["Windrichtung_Grad"], CultureInfo.InvariantCulture));
                Assert.Equal(2.0, Convert.ToDouble(z["Windgeschwindigkeit_m_s"], CultureInfo.InvariantCulture));
                Assert.Equal(3.0, Convert.ToDouble(z["Wasserdampfgehalt_g_kg"], CultureInfo.InvariantCulture));
                Assert.Equal(300.0, Convert.ToDouble(z["Ausstrahlung_W_m2"], CultureInfo.InvariantCulture));
                Assert.Equal(1L, Convert.ToInt64(z["Qualitaet_Strahlung"], CultureInfo.InvariantCulture));
                Assert.Equal(DBNull.Value, z["Global_Horizontal_W_m2"]);
                Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM WHERE ID_Klimaregion = " + mit.Id +
                                      " AND Stunde = 11 AND Direkt_Horizontal_W_m2 = 100 AND Diffus_Horizontal_W_m2 = 50"));

                // Projektkopie (KD-Q4): die Herkunft wandert mit, die Rohwerte nicht.
                long vorher = Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM");
                int idProjektregion = KlimaregionStammCtrl.CopyRegionToProjekt(mit.Id, 1030);
                Assert.True(idProjektregion > 0);
                Assert.Equal("datei", Text("SELECT Bezugspunkt_Art FROM Tab_Klimaregion WHERE ID = " + idProjektregion));
                Assert.Equal(8760L, Zahl("SELECT COUNT(*) FROM Tab_Solar WHERE ID_Klimaregion = " + idProjektregion));
                Assert.Equal(vorher, Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM"));
                Assert.False(DataRepository.TabelleVorhanden("Tab_Klimastunde"));

                // Ohne Rohtabelle (der geparkte Stand ohne Hebung): benannt uebersprungen, Rechenspalten gleich.
                Ausfuehren("DROP TABLE Tab_Klimastunde_STAMM");
                Assert.False(KlimaRohwerteSchema.SchemaVollstaendig());
                KlimaImportErgebnis ohne = await KlimaImportAblauf.Laufen(Dateiauftrag("KD4 ohne Rohwerte", datei), null);
                Assert.True(ohne.Erfolgreich, ohne.Meldung);
                Assert.Equal(0, ohne.Rohstunden);
                Assert.Equal(0L, Zahl("SELECT Rohdaten FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + ohne.Id));
                Assert.Equal(Rechenspalten(mit.Id, "Tab_Solar_STAMM", "ID"), Rechenspalten(ohne.Id, "Tab_Solar_STAMM", "ID"));
                Assert.Equal(Rechenspalten(mit.Id, "Tab_Klimadaten_STAMM", "ID_Klimadaten"),
                             Rechenspalten(ohne.Id, "Tab_Klimadaten_STAMM", "ID_Klimadaten"));
                Assert.DoesNotContain(WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_ROHWERTE_GESPEICHERT,
                                      Text("SELECT Details FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + ohne.Id));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        [Fact]
        public async Task PVGIS_Import_schreibt_Rohwerte_in_UTC_und_den_PVGIS_Punkt()
        {
            if (!_db.Vorhanden) return;
            string json = File.ReadAllText(Probe("pvgis_tmy_stuttgart_72h.json"));
            ITmyQuelle tmy = (lon, lat, az) => Task.FromResult(PVGIS_EPW_Downloader.AusJson(json, out _));
            KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusKoordinaten,
                Bezeichnung = "KD4 PVGIS",
                Longitude = 9.1829,
                Latitude = 48.7758
            }, tmy);
            Assert.True(erg.Erfolgreich, erg.Meldung);
            Assert.Equal(72, erg.Rohstunden);
            Assert.Equal("UTC", Text("SELECT Zeitbezug FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + erg.Id));
            Assert.Equal("pvgis", Text("SELECT Bezugspunkt_Art FROM Tab_Klimaregion_STAMM WHERE ID_Klimaregion = " + erg.Id));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Klimastunde_STAMM WHERE ID_Klimaregion = " + erg.Id +
                                  " AND Stunde = 1 AND Luftdruck_hPa = 1013 AND Windrichtung_Grad = 180 AND Bedeckungsgrad_Achtel IS NULL"));
        }

        // =====================================================================
        //  Nachzuege aus KD-3
        // =====================================================================

        [Fact]
        public async Task Ortsweg_uebernimmt_die_Bezeichnung_als_Regionsnamen()
        {
            if (!_db.Vorhanden) return;
            string json = File.ReadAllText(Probe("pvgis_tmy_stuttgart_72h.json"));
            ITmyQuelle tmy = (lon, lat, az) => Task.FromResult(PVGIS_EPW_Downloader.AusJson(json, out _));
            IOrtsQuelle orte = ort => Task.FromResult((true, 48.7758, 9.1829, "Stuttgart, Probe"));

            KlimaImportErgebnis benannt = await KlimaImportAblauf.Laufen(new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusOrtsname, Ortsname = "KD4 Ort A", Bezeichnung = "  KD4 eigener Name  "
            }, tmy, orte);
            Assert.True(benannt.Erfolgreich, benannt.Meldung);
            Assert.Equal("KD4 eigener Name", benannt.Bezeichner);
            Assert.True(new KlimaregionStammCtrl().GetStammId("KD4 eigener Name") > 0);
            Assert.False(new KlimaregionStammCtrl().GetStammId("KD4 Ort A") > 0);

            KlimaImportErgebnis leer = await KlimaImportAblauf.Laufen(new KlimaImportAuftrag
            {
                Art = KlimaImportArt.AusOrtsname, Ortsname = "KD4 Ort B", Bezeichnung = "   "
            }, tmy, orte);
            Assert.True(leer.Erfolgreich, leer.Meldung);
            Assert.Equal("KD4 Ort B", leer.Bezeichner);
        }

        [Fact]
        public void Parameterbestand_sagt_mit_Rohtabelle_was_gespeichert_wird()
        {
            IReadOnlyList<KlimaParameterEintrag> ohne = KlimaParameterbestand.Fuer(KlimaQuelle.TryDatei);
            Assert.True(Eintrag(ohne, KlimaParameter.Lufttemperatur).Gespeichert);
            Assert.True(Eintrag(ohne, KlimaParameter.Globalstrahlung).Gespeichert);
            Assert.False(Eintrag(ohne, KlimaParameter.Luftdruck).Gespeichert);
            Assert.False(Eintrag(ohne, KlimaParameter.DirektstrahlungHorizontal).Gespeichert);

            IReadOnlyList<KlimaParameterEintrag> mit = KlimaParameterbestand.Fuer(KlimaQuelle.TryDatei, null, rohtabelle: true);
            Assert.All(mit, e => Assert.True(e.Gespeichert, e.Parameter.ToString()));

            IReadOnlyList<KlimaParameterEintrag> pvgis = KlimaParameterbestand.Fuer(KlimaQuelle.PvgisTmy, null, rohtabelle: true);
            Assert.True(Eintrag(pvgis, KlimaParameter.Windrichtung).Gespeichert);
            Assert.False(Eintrag(pvgis, KlimaParameter.Bedeckungsgrad).Gespeichert);
            Assert.False(Eintrag(pvgis, KlimaParameter.Wasserdampfgehalt).Gespeichert);
        }

        // =====================================================================
        //  Leser des Reiters „Weitere Reihen“ (KD-8 Teil B)
        // =====================================================================

        /// <summary>
        /// Teil B: Mit dem Schritt 238 sind die Rohwerte freigeschaltet (Datenstand). Der Leser
        /// <see cref="KlimaExportCtrl.RohwerteLesen"/> liefert allein die Rohwerte einer Katalogregion — leer für den
        /// Altbestand (<c>Rohdaten = 0</c>, KD-Q7) —, und <see cref="KlimaReihenkatalog.AusRohwerten"/> macht daraus die fünf
        /// Reihen des Jahresverlaufs, geführt oder weich gesperrt.
        /// </summary>
        [Fact]
        public void RohwerteLesen_und_Reihen_des_Jahresverlaufs_folgen_dem_Datenstand()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KlimaReihenkatalog.RohwerteFreigeschaltet());

            long id = Zahl("SELECT MIN(ID_Klimaregion) FROM Tab_Klimaregion_STAMM");
            KlimaExportDaten ohne = KlimaExportCtrl.RohwerteLesen((int)id);
            Assert.NotNull(ohne);
            Assert.False(ohne.Rohdaten);                       // Altbestand (KD-Q7): nichts nachgeladen
            Assert.Empty(ohne.Rohwerte);
            Assert.Empty(ohne.Rechenwerte);                   // allein die Rohwerte, die Rechenspalten hat die Hülle
            IReadOnlyList<KlimaReihenwerte> gesperrt = KlimaReihenkatalog.AusRohwerten(ohne);
            Assert.Equal(5, gesperrt.Count);
            Assert.All(gesperrt, w => Assert.False(w.Gefuehrt));

            // Eine Region mit Rohwerten: 24 Stunden Luftdruck, Kennzeichen und Zeitbezug gesetzt.
            for (int h = 1; h <= 24; h++)
                Ausfuehren("INSERT INTO Tab_Klimastunde_STAMM (ID_Klimaregion, Stunde, Luftdruck_hPa) VALUES (" + id + ", " +
                           h + ", " + (1000 + h) + ")");
            Ausfuehren("UPDATE Tab_Klimaregion_STAMM SET Rohdaten = 1, Zeitbezug = 'MEZ' WHERE ID_Klimaregion = " + id);
            KlimaExportDaten mit = KlimaExportCtrl.RohwerteLesen((int)id);
            Assert.True(mit.Rohdaten);
            Assert.Equal(KlimaRohwerteSchema.ZEITBEZUG_MEZ, mit.RohZeitbezug);
            Assert.Equal(1001.0, mit.Rohwerte[KlimaRohwerteSchema.SPALTE_LUFTDRUCK][0]);
            Assert.True(double.IsNaN(mit.Rohwerte[KlimaRohwerteSchema.SPALTE_LUFTDRUCK][24]));
            IReadOnlyList<KlimaReihenwerte> reihen = KlimaReihenkatalog.AusRohwerten(mit);
            Assert.True(reihen.Single(w => w.Reihe.Schluessel == "ROH_Luftdruck_hPa").Gefuehrt);
            Assert.False(reihen.Single(w => w.Reihe.Schluessel == "ROH_Wasserdampfgehalt_g_kg").Gefuehrt);
            Assert.Equal(mit.Rohdaten, KlimaExportCtrl.Lesen((int)id).Rohdaten);   // der Export liest dieselbe Auskunft
            Assert.Null(KlimaExportCtrl.RohwerteLesen(987654));
        }

        // =====================================================================
        //  Wache: der Rechenweg liest die Rohtabelle nicht
        // =====================================================================

        /// <summary>
        /// <b>Kein Rechenweg liest die Rohwerte</b> (Konzept § 6): Die Rohtabelle und ihre Typen nennt im Kern nur der
        /// Import, der Schemaschritt, das Katalogregister und (später) der Export — keine Datei unter
        /// <c>Allgemein/Simulation/**</c>, <c>Wirtschaftlichkeit/**</c>, <c>Bericht/**</c> und nicht <c>BhkwPlan.cs</c>.
        /// </summary>
        [Fact]
        public void Wache_Rechenweg_liest_die_Rohtabelle_nicht()
        {
            string wurzel = Repowurzel();
            if (wurzel == null) return;
            string kern = Path.Combine(wurzel, "EPOS.Kern");
            string[] merkmale = { "Tab_Klimastunde", "KlimaRohwerteSchema.TAB_ROH", "KlimaRohstunde", "KlimaRohreihe", "RohwerteSchreiben" };
            string[] erlaubt =
            {
                "Allgemein/Update/", "Allgemein/Import/", "Allgemein/Katalog/", "Allgemein/Export/",
                // KD-5: der Leser der Rohtabelle ist der Export - seine Datenseite steht als Controller.
                "Controller/KlimaExportCtrl.cs"
            };

            var treffer = new List<string>();
            foreach (string datei in Directory.EnumerateFiles(kern, "*.cs", SearchOption.AllDirectories))
            {
                string rel = Path.GetRelativePath(kern, datei).Replace('\\', '/');
                if (rel.StartsWith("bin/", StringComparison.Ordinal) || rel.StartsWith("obj/", StringComparison.Ordinal)) continue;
                string text = File.ReadAllText(datei);
                if (!merkmale.Any(m => text.Contains(m, StringComparison.Ordinal))) continue;
                if (!erlaubt.Any(e => rel.StartsWith(e, StringComparison.Ordinal))) treffer.Add(rel);
            }
            Assert.True(treffer.Count == 0, "Rohwerte ausserhalb von Import/Schema/Katalog/Export: " + string.Join(", ", treffer));

            // Und ausdruecklich: Simulation und BhkwPlan nennen auch die Schemaklasse nicht.
            foreach (string datei in Directory.EnumerateFiles(Path.Combine(kern, "Allgemein", "Simulation"), "*.cs",
                                                              SearchOption.AllDirectories)
                                              .Append(Path.Combine(kern, "Allgemein", "BhkwPlan.cs")))
                if (File.Exists(datei))
                    Assert.DoesNotContain("KlimaRohwerteSchema", File.ReadAllText(datei));
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        private static KlimaParameterEintrag Eintrag(IReadOnlyList<KlimaParameterEintrag> l, KlimaParameter p) =>
            l.Single(e => e.Parameter == p);

        private static KlimaImportAuftrag Dateiauftrag(string bezeichnung, string datei) => new KlimaImportAuftrag
        {
            Art = KlimaImportArt.AusKoordinaten,
            Bezeichnung = bezeichnung,
            Longitude = 9.18,
            Latitude = 48.78,
            Quelle = KlimaQuelle.TryDatei,
            TryPfad = datei
        };

        /// <summary>Ein volles TRY-Jahr mit erfundenen Werten (17 Felder je Zeile).</summary>
        private static string Jahresreihe()
        {
            var sb = new StringBuilder(800 * 1024);
            sb.Append("Probe Testreferenzjahr KD-4 - erfundene Werte\r\n");
            sb.Append("Art des TRY: Jahr\r\n");
            sb.Append("Bezugszeitraum: 1995-2012\r\n");
            sb.Append("Hoehenlage: 300 Meter ueber NN\r\n");
            sb.Append("*** \r\n");
            int[] tageMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            for (int m = 1; m <= 12; m++)
                for (int tag = 1; tag <= tageMonat[m - 1]; tag++)
                    for (int h = 1; h <= 24; h++)
                    {
                        double t = m <= 6 ? 5.0 : 20.0;
                        int b = h >= 10 && h <= 17 ? 100 : 0;
                        int d = b > 0 ? 50 : 0;
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "4321000 5678000 {0} {1} {2} {3:F1} 1013 180 2.0 4 3.0 80 {4} {5} 260 300 1\r\n",
                            m, tag, h, t, b, d);
                    }
            return sb.ToString();
        }

        /// <summary>Die Rechenspalten einer Stammregion als ein Text, in Zeilenfolge.</summary>
        private static string Rechenspalten(int id, string tabelle, string ordnung)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT group_concat(Zeile, '|') FROM (SELECT Temperatur || ';' || Globalstrahlung || ';' || " +
                "Direktstrahlung || ';' || Diffusstrahlung || ';' || Sol_Sued || ';' || Sol_Ost || ';' || " +
                "Sonnenwinkel AS Zeile FROM " + tabelle + " WHERE ID_Klimaregion = ? ORDER BY " + ordnung + ")",
                new DbParam("@id", id));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static string Ddl(string tabelle) => Convert.ToString(DataRepository.ExecuteScalar(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("@n", tabelle)),
            CultureInfo.InvariantCulture) ?? "";

        private static void Ausfuehren(string sql)
        {
            using DbVorgang v = DataRepository.Vorgang();
            v.Ausfuehren(sql);
            v.Commit();
        }

        private static long Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static double Reell(string sql) =>
            Convert.ToDouble(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static string Text(string sql) =>
            Convert.ToString(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture) ?? "";

        private static string Probe(string datei)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben", datei);
                if (File.Exists(kandidat)) return kandidat;
            }
            throw new FileNotFoundException(datei);
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
