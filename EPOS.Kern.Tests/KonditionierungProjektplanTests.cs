using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Planwege tragen die Konditionierung</b> (Stufe KP1b, Konzept
    /// Konditionierungsprofile 5.5): Projekt duplizieren, Variante anlegen, der
    /// <c>.wpx</c>-Rundlauf und die Anhebung eines Pakets vom Stand
    /// <see cref="KonditionierungSchema.SCHRITT"/> auf
    /// <see cref="KonditionierungVorlagenSchema.SCHRITT"/>.
    ///
    /// <para><b>Gebaut ist dafür nichts</b> — die drei Tabellen stehen seit KP1a im
    /// <c>KINDER</c>-Plan (<c>ProjektDuplizierenCtrl</c>). Diese Fälle sind der NACHWEIS: Gebäude-
    /// und Zonenkalender, Perioden und Vorgabezeilen reisen mit und werden umgeschlüsselt, während
    /// die Zeilen eines Katalogbaus und einer Vorlage beim Katalog bleiben (P3 (b), P11).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KonditionierungProjektplanTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly KonditionierungCtrl _ctrl = new KonditionierungCtrl();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar();

        // =============================================================================
        //  Duplikat und Variante
        // =============================================================================

        [Fact]
        public void Ein_Projektduplikat_nimmt_Gebaeude_und_Zonenkalender_umgeschluesselt_mit()
        {
            if (!Bereit()) return;
            Aufbau a = Aufbauen();
            if (a == null) return;

            int neu = new ProjektDuplizierenCtrl().Duplizieren(a.Projektname, "D1 Duplikat");
            Assert.True(neu > 0);

            Pruefen(neu, a);
        }

        [Fact]
        public void Eine_Variante_nimmt_dieselben_Zeilen_mit()
        {
            if (!Bereit()) return;
            Aufbau a = Aufbauen();
            if (a == null) return;

            int neu = new VariantenCtrl().AnlegenAusStamm(a.Projekt, a.Projektname, "D1", out string fehler);
            Assert.True(neu > 0, fehler);

            Pruefen(neu, a);
        }

        // =============================================================================
        //  Der .wpx-Rundlauf
        // =============================================================================

        [Fact]
        public void Der_wpx_Rundlauf_traegt_Kalender_Perioden_und_Vorgaben()
        {
            if (!Bereit()) return;
            Aufbau a = Aufbauen();
            if (a == null) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("kond.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(a.Projektname, paket));

            int neu = io.Importieren(paket, "D1 Rundlauf", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, fehler);

            Pruefen(neu, a);
        }

        [Fact]
        public void Ein_Paket_auf_Stand_151_wird_gehoben_und_traegt_seine_Kalender()
        {
            if (!Bereit()) return;
            Aufbau a = Aufbauen();
            if (a == null) return;
            using var ordner = new Arbeitsordner();

            string paket = ordner.Datei("neu.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(a.Projektname, paket));

            // Schritt 152 ist reines DDL: Das Paket eines Stands 151 trägt dieselben Zeilen und
            // muss nach der Anhebung unverändert dastehen.
            string alt = ordner.Datei("alt.wpx");
            StandSetzen(paket, alt, KonditionierungSchema.SCHRITT);

            int neu = io.Importieren(alt, "D1 Anhebung 151", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, fehler);
            // Der Bericht nennt die Anhebung von 151 auf den Zielstand (152 und jeder spätere
            // Schritt); ein reiner DDL-Schritt bringt keine eigene Zeile, weil er an den
            // Paketdaten nichts umformt.
            string von = KonditionierungSchema.SCHRITT.ToString(CultureInfo.InvariantCulture);
            string bis = SchemaStand.Zielversion.ToString(CultureInfo.InvariantCulture);
            Assert.Contains(io.LetzterBericht,
                            z => z.Contains(von, StringComparison.Ordinal)
                                 && z.Contains(bis, StringComparison.Ordinal));

            Pruefen(neu, a);
        }

        // =============================================================================
        //  Aufbau und Prüfung
        // =============================================================================

        /// <summary>Was der Aufbau angelegt hat — und was die Prüfung im Ziel erwartet.</summary>
        private sealed class Aufbau
        {
            public int Projekt;
            public string Projektname;
            public long Gebaeude;
            public long Zone;
            public long KatalogVorgabenVorher;
            public long VorlagenVorgabenVorher;
        }

        /// <summary>
        /// Ein Projekt mit einem Gebäude, einer Zone, je einer Vorgabezeile und je einem Kalender
        /// samt Perioden — dazu Zeilen an einem Katalogbau und (wenn es die Tabelle gibt) an einer
        /// Vorlage, die NICHT mitreisen dürfen.
        /// </summary>
        private Aufbau Aufbauen()
        {
            long gebaeude = Id("SELECT MIN(ID) FROM \"Tab_Gebaeude\" WHERE \"ID_Projekt\" IS NOT NULL");
            if (gebaeude == 0) return null;
            int projekt = (int)Id("SELECT \"ID_Projekt\" FROM \"Tab_Gebaeude\" WHERE \"ID\" = " +
                                  gebaeude.ToString(CultureInfo.InvariantCulture));
            if (projekt <= 0) return null;
            string name = Text("SELECT \"Projektname\" FROM \"Tab_Projekt\" WHERE \"ID\" = " +
                               projekt.ToString(CultureInfo.InvariantCulture));
            if (string.IsNullOrEmpty(name)) return null;

            DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"IstBeheizt\") " +
                "VALUES (?, 1, ?, 1)",
                new DbParam("@g", gebaeude), new DbParam("@b", "Planprobe"));
            long zone = Id("SELECT MAX(ID) FROM \"Tab_Zone\"");

            KonditionierungCtrl.Eigner g = KonditionierungCtrl.Eigner.Gebaeude(gebaeude);
            KonditionierungCtrl.Eigner z = KonditionierungCtrl.Eigner.Zone(gebaeude, zone);
            VorgabeSchreiben(g, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.8);
            VorgabeSchreiben(z, Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.3);
            Assert.True(_ctrl.Schreiben(g, Heizkalender(20.0)).Ok);
            Assert.True(_ctrl.Schreiben(z, Heizkalender(19.0)).Ok);

            // Was dem KATALOG gehört, reist nicht mit (P3 (b), P11).
            long stamm = Id("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\" WHERE \"ReadOnly\" = 0");
            VorgabeSchreiben(KonditionierungCtrl.Eigner.Katalogbau(stamm),
                             Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_NACHT, 0.9);
            long vorlagenzeilen = 0;
            if (KonditionierungVorlagenSchema.Lesbar())
            {
                DataRepository.ExecuteSQL(
                    "INSERT INTO \"" + KonditionierungVorlagenSchema.TAB_VORLAGE +
                    "\" (\"Groesse\", \"Bezeichner\", \"ReadOnly\") VALUES (?, ?, 0)",
                    new DbParam("@g", DbWerte.KOND_GROESSE_LUEFTUNG), new DbParam("@b", "D1 Planprobe"));
                long vorlage = Id("SELECT MAX(ID) FROM \"" + KonditionierungVorlagenSchema.TAB_VORLAGE + "\"");
                VorgabeSchreiben(KonditionierungCtrl.Eigner.Vorlage(vorlage),
                                 Konditionierungsgroesse.Lueftung, DbWerte.KOND_ZEILE_TAG, 0.7);
                vorlagenzeilen = Anzahl(KonditionierungSchema.TAB_VORGABE, "\"ID_Vorlage\" IS NOT NULL");
            }

            return new Aufbau
            {
                Projekt = projekt,
                Projektname = name,
                Gebaeude = gebaeude,
                Zone = zone,
                KatalogVorgabenVorher = Anzahl(KonditionierungSchema.TAB_VORGABE,
                                               "\"ID_Gebaeude_Stamm\" IS NOT NULL"),
                VorlagenVorgabenVorher = vorlagenzeilen,
            };
        }

        /// <summary>Im Zielprojekt stehen dieselben Zeilen — umgeschlüsselt auf seine Ids.</summary>
        private void Pruefen(int neuesProjekt, Aufbau a)
        {
            long gebaeudeNeu = Id("SELECT MIN(ID) FROM \"Tab_Gebaeude\" WHERE \"ID_Projekt\" = " +
                                  neuesProjekt.ToString(CultureInfo.InvariantCulture));
            Assert.True(gebaeudeNeu > 0);
            Assert.NotEqual(a.Gebaeude, gebaeudeNeu);

            long zoneNeu = Id("SELECT MIN(ID) FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = " +
                              gebaeudeNeu.ToString(CultureInfo.InvariantCulture));
            Assert.True(zoneNeu > 0);
            Assert.NotEqual(a.Zone, zoneNeu);

            KonditionierungCtrl.Eigner g = KonditionierungCtrl.Eigner.Gebaeude(gebaeudeNeu);
            KonditionierungCtrl.Eigner z = KonditionierungCtrl.Eigner.Zone(gebaeudeNeu, zoneNeu);

            // Die Vorgabezeilen — Gebäude und Zone getrennt, mit ihren Werten.
            List<Vorgabezeile> gz = _ctrl.Vorgaben(g);
            Assert.Single(gz);
            Assert.Equal(0.8, gz[0].Wert.Value, 9);
            List<Vorgabezeile> zz = _ctrl.Vorgaben(z);
            Assert.Single(zz);
            Assert.Equal(0.3, zz[0].Wert.Value, 9);

            // Die Kalender samt Perioden — ID_Kalender ist umgeschlüsselt, sonst fände der
            // strenge Leser die Perioden nicht.
            Konditionierungskalender kg = _ctrl.Kalender(g, out string mg)[Konditionierungsgroesse.Heizsoll];
            Assert.Null(mg);
            Assert.Equal(2, kg.Perioden.Count);
            Assert.Equal(20.0, kg.Standardwoche[0], 9);

            Konditionierungskalender kz = _ctrl.Kalender(z, out string mz)[Konditionierungsgroesse.Heizsoll];
            Assert.Null(mz);
            Assert.Equal(2, kz.Perioden.Count);
            Assert.Equal(19.0, kz.Standardwoche[0], 9);

            // Katalog- und Vorlagenzeilen bleiben, wo sie waren.
            Assert.Equal(a.KatalogVorgabenVorher,
                         Anzahl(KonditionierungSchema.TAB_VORGABE, "\"ID_Gebaeude_Stamm\" IS NOT NULL"));
            Assert.Equal(a.VorlagenVorgabenVorher,
                         Anzahl(KonditionierungSchema.TAB_VORGABE, "\"ID_Vorlage\" IS NOT NULL"));
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        /// <summary>Setzt den Schemastand im Manifest eines Pakets; alles Übrige bleibt byte-gleich.</summary>
        private static void StandSetzen(string quelle, string ziel, int stand)
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
                using Stream s = aus.CreateEntry(kvp.Key, CompressionLevel.Optimal).Open();
                s.Write(roh, 0, roh.Length);
            }
        }

        private static Konditionierungskalender Heizkalender(double wert)
        {
            var woche = new double[168];
            for (int i = 0; i < woche.Length; i++) woche[i] = wert;
            return new Konditionierungskalender(
                Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWoche(woche), null,
                new[]
                {
                    Kalenderregel.Zeitraum(Standardfahrplan.RANG_FERIEN, DbWerte.KOND_ART_FERIEN,
                                           "Ferien 1", 100, 110, Kalenderangabe.AusWert(16.0)),
                    Kalenderregel.Zeitraum(400, DbWerte.KOND_ART_ZEITRAUM,
                                           "Eigene Zeile", 200, 210, Kalenderangabe.AusWert(18.0)),
                });
        }

        private static void VorgabeSchreiben(KonditionierungCtrl.Eigner e, Konditionierungsgroesse g,
                                             string zeile, double? wert)
        {
            var parameter = new List<DbParam>(e.Spaltenwerte("@e"))
            {
                new DbParam("@gr", Konditionierungsgroessen.Kennwort(g)),
                new DbParam("@ze", zeile),
                new DbParam("@we", (object)wert),
            };
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE +
                "\" (\"ID_Gebaeude\", \"ID_Zone\", \"ID_Gebaeude_Stamm\", \"ID_Vorlage\", " +
                "\"Groesse\", \"Zeile\", \"Wert\", \"Aus\") VALUES (?, ?, ?, ?, ?, ?, ?, 0)",
                parameter.ToArray()));
        }

        private static long Anzahl(string tabelle, string bedingung)
            => Id("SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE " + bedingung);

        private static long Id(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? null : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Ein Ordner für die Paketdateien eines Falls; er räumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad = Path.Combine(Path.GetTempPath(),
                "epos-kondplan-" + Guid.NewGuid().ToString("N").Substring(0, 8));

            public Arbeitsordner() => Directory.CreateDirectory(_pfad);

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }
    }
}
