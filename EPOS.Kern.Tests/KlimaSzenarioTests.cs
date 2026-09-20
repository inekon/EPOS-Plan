using System;
using System.Collections.Generic;
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
    /// <b>SCHEMASCHRITT 97 — Szenario und Bezugsjahr der Klimaregion</b>
    /// (Anwenderentscheid vom 19.09.2026: „Wird die Quelle und Auswahl (z. B. TRY 2045
    /// sommerwarm …) der Klimadaten angezeigt? Diese sollte auch bei der Klimaregion
    /// sichtbar sein."; Auftrag KL-6).
    ///
    /// <para><b>Worum es geht.</b> Welches Wetterjahr eine Reihe beschreibt, stand bis
    /// hierher nur im Freitext <c>Details</c> — lesbar, aber nicht auswertbar. Ab
    /// Schritt 97 führen <c>Tab_Klimaregion</c> und <c>Tab_Klimaregion_STAMM</c> das
    /// <c>Szenario</c> als Schlüssel und das <c>Bezugsjahr</c> als Zahl, und beide
    /// stehen in der Regionsliste und auf der Startseite.</para>
    ///
    /// <para><b>Was hier festgehalten wird.</b> Sechs Dinge: die vier Spalten und ihre
    /// Wiederholbarkeit, der NULL gebliebene Bestand, das Schreiben beim Import, die
    /// Projektkopie, der zusammengesetzte Satz der Liste und der Eintragstext des
    /// Auswahlfeldes — jeweils in BEIDEN Kulturen, wo ein Anzeigetext im Spiel
    /// ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KlimaSzenarioTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        /// <summary>Ein Projekt der Testdatenbank — Ziel der Projektkopie.</summary>
        private const int PROJEKT = 1030;

        /// <summary>Sein Name — der führende Schlüssel des Speicherwegs.</summary>
        private const string NAME_PROJEKT = "Referenz BHKW-Kaskade (Regressionstest)";

        // =====================================================================
        //  1 — Die vier Spalten
        // =====================================================================

        /// <summary>
        /// Die LISTE des Schrittes, eingefroren: Szenario und Bezugsjahr, je einmal im
        /// Katalog und einmal in der Projektkopie. Eine Spalte nur auf einer Seite wäre
        /// beim Kopieren ins Projekt sofort ein Datenverlust.
        /// </summary>
        [Fact]
        public void Der_Schritt_fuehrt_vier_Spalten_an_zwei_Tabellen()
        {
            SchemaSpalte[] spalten = SchemaKatalog.Schritt97_KlimaSzenario;

            Assert.Equal(4, spalten.Length);

            foreach (string tabelle in new[] { SchemaKatalog.TAB_KLIMAREGION,
                                               SchemaKatalog.TAB_KLIMAREGION_STAMM })
            {
                Assert.Contains(spalten, s => s.Tabelle == tabelle &&
                                              s.Name == SchemaKatalog.SPALTE_KR_SZENARIO &&
                                              s.TypDefinition == "TEXT(12)");
                Assert.Contains(spalten, s => s.Tabelle == tabelle &&
                                              s.Name == SchemaKatalog.SPALTE_KR_BEZUGSJAHR &&
                                              s.TypDefinition == "LONG");
            }

            Assert.Equal(97, SchemaStand.Zielversion);
        }

        /// <summary>
        /// Nach dem Aufbau der Arbeitskopie stehen alle vier Spalten — und ein zweiter
        /// Lauf des Schrittes fasst nichts mehr an: Die Anlegeprüfung fragt
        /// <c>PRAGMA table_info</c>, SQLite kennt kein <c>ADD COLUMN IF NOT EXISTS</c>.
        /// </summary>
        [Fact]
        public void Die_Spalten_stehen_und_der_Schritt_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt97_KlimaSzenario)
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name),
                            s.Tabelle + "." + s.Name + " fehlt.");

            // Zweiter Lauf: Es ist nichts mehr offen.
            int offen = 0;
            foreach (SchemaSpalte s in SchemaKatalog.Schritt97_KlimaSzenario)
                if (!DataRepository.SpalteVorhanden(s.Tabelle, s.Name)) offen++;

            Assert.Equal(0, offen);
        }

        /// <summary>
        /// <b>Der Bestand bleibt NULL</b> — der Schritt trägt kein DML. NULL heißt
        /// „sagt nichts dazu"; nachdatiert wird nichts, und aus NULL wird keine 0 und
        /// kein „mittleres Jahr".
        /// </summary>
        [Fact]
        public void Der_Bestand_bleibt_in_beiden_Spalten_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt97_KlimaSzenario)
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" +
                                     s.Name + "] IS NOT NULL"));
        }

        // =====================================================================
        //  2 — Die Schlüssel
        // =====================================================================

        /// <summary>
        /// Der sprachneutrale Schlüssel je Szenario, eingefroren (Drei-Schichten-Regel:
        /// In der Datenbank steht ein SCHLÜSSEL, kein Anzeigetext).
        /// </summary>
        [Theory]
        [InlineData(TrySzenario.MittleresJahr, "MITTEL")]
        [InlineData(TrySzenario.Sommerwarm, "SOMMERWARM")]
        [InlineData(TrySzenario.Winterkalt, "WINTERKALT")]
        public void Jedes_Szenario_traegt_seinen_sprachneutralen_Schluessel(TrySzenario s, string schluessel)
        {
            Assert.Equal(schluessel, KlimaImportAblauf.Szenarioschluessel(s));
        }

        /// <summary>
        /// <b>Der Dateikopf sagt das Szenario — oder nichts.</b> „Art des TRY" IST das
        /// Szenario; gemessen wird gegen die tragenden Wortteile, ohne Rücksicht auf
        /// Schreibweise, Bindestrich und Zwischenraum. Was sich nicht zuordnen lässt,
        /// bleibt LEER — eine Vorgabe wäre hier eine Behauptung über die Datei.
        /// </summary>
        [Theory]
        [InlineData("Sommer warm", "SOMMERWARM")]
        [InlineData("sommerwarm", "SOMMERWARM")]
        [InlineData("Sommer-warm", "SOMMERWARM")]
        [InlineData("Winter kalt", "WINTERKALT")]
        [InlineData("mittleres Jahr", "MITTEL")]
        [InlineData("Jahr", "")]
        [InlineData("", "")]
        [InlineData(null, "")]
        public void Der_Dateikopf_sagt_das_Szenario_oder_nichts(string art, string erwartet)
        {
            Assert.Equal(erwartet, KlimaImportAblauf.SzenarioschluesselAusKopf(art));
        }

        /// <summary>
        /// <b>Der Bezugszeitraum des Kopfs ergibt das Bezugsjahr — oder nichts</b>
        /// (Auftrag KL-7, Anwenderentscheid vom 20.09.2026: DWD-Konvention). Die
        /// Zuordnung ist eine TABELLE, keine Schwelle: 1995-2012 ist 2015, 2031-2060
        /// ist 2045. Womit die beiden Jahreszahlen verbunden sind, ist gleichgültig.
        /// Jeder andere Zeitraum — ein einzelnes Jahr, ein fremder Zeitraum, gar
        /// nichts — bleibt LEER und wird NIE zur Vorgabe 2015.
        /// </summary>
        [Theory]
        [InlineData("1995-2012", 2015)]
        [InlineData("1995 - 2012", 2015)]
        [InlineData("1995–2012", 2015)]
        [InlineData("1995 bis 2012", 2015)]
        [InlineData("2031-2060", 2045)]
        [InlineData("2031 bis 2060", 2045)]
        [InlineData("", null)]
        [InlineData(null, null)]
        [InlineData("2012", null)]
        [InlineData("1961-1990", null)]
        [InlineData("keine Angabe", null)]
        public void Der_Bezugszeitraum_des_Kopfs_ergibt_das_Jahr_oder_nichts(string zeitraum, int? erwartet)
        {
            Assert.Equal(erwartet, KlimaImportAblauf.BezugsjahrAusZeitraum(zeitraum));
        }

        // =====================================================================
        //  3 — Schreiben: Import und Projektkopie
        // =====================================================================

        /// <summary>
        /// <b>Der Import einer TRY-Datei schreibt Szenario UND Bezugsjahr aus dem
        /// Kopf</b> (Auftrag KL-7): „Art des TRY" ist das Szenario, der
        /// „Bezugszeitraum" ergibt nach der DWD-Konvention das Bezugsjahr — hier
        /// 2031-2060 → 2045. Der gelesene Zeitraum steht wörtlich im
        /// Herkunftsvermerk, damit die Ableitung nachlesbar bleibt.
        /// </summary>
        [Fact]
        public async Task Der_TRY_Dateiimport_schreibt_Szenario_und_Jahr_aus_dem_Kopf()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = TryDatei("Art des TRY: Sommer warm", "Bezugszeitraum: 2031-2060");
            try
            {
                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "KL7 TRY Probe",
                        Longitude = 9.1829,
                        Latitude = 48.7758,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    },
                    null);          // KEINE TMY-Quelle noetig - kein Netz

                Assert.True(erg.Erfolgreich, erg.Meldung);

                Assert.Equal(DbWerte.KLIMA_SZENARIO_SOMMERWARM,
                             Text(SchemaKatalog.SPALTE_KR_SZENARIO, erg.Id));
                Assert.Equal("2045", Text(SchemaKatalog.SPALTE_KR_BEZUGSJAHR, erg.Id));

                Assert.Contains("2031-2060", Text("Details", erg.Id), StringComparison.Ordinal);

                // A-KL7-1: die Regionsliste sagt es im Satzbau aus KL-6.
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_TRY_DATEI + " · 2045 · " +
                             WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM,
                             KlimaregionStammCtrl.Katalogfilterzeilen()
                                 .Single(z => z.Bezeichner == "KL7 TRY Probe")
                                 .Text(Katalogfilterprofil.SpQuelle));
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }
        }

        /// <summary>
        /// <b>Gegenprobe: ohne Bezugszeitraum im Kopf bleibt das Bezugsjahr leer</b>
        /// — auch dann, wenn die Datei ihr Szenario nennt. Ein Jahr, das die Datei
        /// nicht deckt, wäre eine Behauptung über sie; die Vorgabe 2015 gilt allein
        /// für die Regionaldaten, wo der Anwender sie gewählt hat.
        /// </summary>
        [Fact]
        public async Task Ohne_Bezugszeitraum_im_Kopf_bleibt_das_Jahr_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = TryDatei("Art des TRY: Winter kalt");
            try
            {
                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "KL7 TRY ohne Zeitraum",
                        Longitude = 9.1829,
                        Latitude = 48.7758,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    },
                    null);

                Assert.True(erg.Erfolgreich, erg.Meldung);

                Assert.Equal(DbWerte.KLIMA_SZENARIO_WINTERKALT,
                             Text(SchemaKatalog.SPALTE_KR_SZENARIO, erg.Id));
                Assert.Equal("", Text(SchemaKatalog.SPALTE_KR_BEZUGSJAHR, erg.Id));
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }
        }

        /// <summary>
        /// <b>Ein fremder Bezugszeitraum ergibt kein Jahr</b> (A-KL7-1, Gegenprobe):
        /// „1961-1990" steht nicht in der Zuordnungstabelle — also bleibt das
        /// Bezugsjahr leer, statt still auf die Vorgabe zu fallen.
        /// </summary>
        [Fact]
        public async Task Ein_fremder_Bezugszeitraum_ergibt_kein_Jahr()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = TryDatei("Art des TRY: mittleres Jahr", "Bezugszeitraum: 1961-1990");
            try
            {
                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "KL7 TRY fremder Zeitraum",
                        Longitude = 9.1829,
                        Latitude = 48.7758,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    },
                    null);

                Assert.True(erg.Erfolgreich, erg.Meldung);

                Assert.Equal(DbWerte.KLIMA_SZENARIO_MITTEL,
                             Text(SchemaKatalog.SPALTE_KR_SZENARIO, erg.Id));
                Assert.Equal("", Text(SchemaKatalog.SPALTE_KR_BEZUGSJAHR, erg.Id));
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }
        }

        /// <summary>
        /// <b>Ohne „Art des TRY" im Kopf bleibt das Szenario leer</b> — die Datei sagt
        /// es nicht, also sagt es die Datenbank auch nicht. Die QUELLE steht trotzdem:
        /// Sie ist die Wahl des Anwenders, keine Angabe der Datei.
        /// </summary>
        [Fact]
        public async Task Ohne_Art_im_Kopf_bleibt_das_Szenario_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            string pfad = TryDatei();
            try
            {
                KlimaImportErgebnis erg = await KlimaImportAblauf.Laufen(
                    new KlimaImportAuftrag
                    {
                        Art = KlimaImportArt.AusKoordinaten,
                        Bezeichnung = "KL6 TRY ohne Art",
                        Longitude = 9.1829,
                        Latitude = 48.7758,
                        Quelle = KlimaQuelle.TryDatei,
                        TryPfad = pfad
                    },
                    null);

                Assert.True(erg.Erfolgreich, erg.Meldung);

                Assert.Equal("", Text(SchemaKatalog.SPALTE_KR_SZENARIO, erg.Id));
                Assert.Equal("", Text(SchemaKatalog.SPALTE_KR_BEZUGSJAHR, erg.Id));
                Assert.Equal(DbWerte.KLIMA_QUELLE_TRY_DATEI, Text(SchemaKatalog.SPALTE_KR_QUELLE, erg.Id));
            }
            finally
            {
                try { File.Delete(pfad); } catch { /* aufraeumen darf scheitern */ }
            }
        }

        /// <summary>
        /// <b>Die Projektkopie trägt beides mit.</b> Eine Spalte nur auf der
        /// Katalogseite wäre hier sofort ein Datenverlust — das Projekt wüsste nicht
        /// mehr, welches Wetterjahr seine Reihe beschreibt.
        /// </summary>
        [Fact]
        public void Die_Projektkopie_traegt_Szenario_und_Bezugsjahr_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int stammId = RegionAnlegen("KL6 Kopie Probe", DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                                        DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045);
            Assert.True(stammId > 0);

            int projektRegionId;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                projektRegionId = KlimaregionStammCtrl.CopyRegionToProjekt(stammId, PROJEKT, v);
                v.Commit();
            }

            Assert.True(projektRegionId > 0);

            Assert.Equal(DbWerte.KLIMA_SZENARIO_SOMMERWARM,
                         ProjektText(SchemaKatalog.SPALTE_KR_SZENARIO, projektRegionId));
            Assert.Equal("2045", ProjektText(SchemaKatalog.SPALTE_KR_BEZUGSJAHR, projektRegionId));
        }

        /// <summary>
        /// <b>Eine ALTBESTANDS-Region bleibt leer</b> — auch nach dem Kopieren ins
        /// Projekt. Nachdatiert wird nichts.
        /// </summary>
        [Fact]
        public void Eine_Altbestandsregion_bleibt_nach_dem_Kopieren_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(ID_Klimaregion) FROM " + SchemaKatalog.TAB_KLIMAREGION_STAMM);
            if (o == null || o == DBNull.Value) return;
            int stammId = Convert.ToInt32(o, CultureInfo.InvariantCulture);

            int projektRegionId;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                projektRegionId = KlimaregionStammCtrl.CopyRegionToProjekt(stammId, PROJEKT, v);
                v.Commit();
            }

            Assert.True(projektRegionId > 0);

            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM " + SchemaKatalog.TAB_KLIMAREGION +
                                 " WHERE ID = ? AND (" + SchemaKatalog.SPALTE_KR_SZENARIO +
                                 " IS NOT NULL OR " + SchemaKatalog.SPALTE_KR_BEZUGSJAHR +
                                 " IS NOT NULL)", projektRegionId));
        }

        // =====================================================================
        //  4 — Die Regionsliste (A-KL6-1)
        // =====================================================================

        /// <summary>
        /// <b>Die Spalte „Quelle" sagt das ganze Wetterjahr</b> (A-KL6-1): Quelle,
        /// Bezugsjahr und Szenario, gereiht mit „ · ". Ohne Szenario und Jahr bleibt es
        /// bei der Quelle allein — genau das, was vor Schritt 97 dastand.
        /// </summary>
        [Fact]
        public void Die_Liste_zeigt_Quelle_Jahr_und_Szenario()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            RegionAnlegen("KL6 Liste voll", DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                          DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045);
            RegionAnlegen("KL6 Liste nur Quelle", DbWerte.KLIMA_QUELLE_PVGIS, null, null);

            IReadOnlyList<Katalogfilterzeile> zeilen = KlimaregionStammCtrl.Katalogfilterzeilen();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_TRY_REGIONAL + " · 2045 · " +
                         WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM,
                         zeilen.Single(z => z.Bezeichner == "KL6 Liste voll")
                               .Text(Katalogfilterprofil.SpQuelle));

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_PVGIS,
                         zeilen.Single(z => z.Bezeichner == "KL6 Liste nur Quelle")
                               .Text(Katalogfilterprofil.SpQuelle));
        }

        /// <summary>
        /// <b>Der Altbestand bekommt den Halbgeviertstrich</b> (A-KL6-3, W6-E-1): Eine
        /// Region ohne Quelle sagt nichts — und „nichts" heißt in der Katalogliste „–",
        /// nie eine erfundene Quelle.
        /// </summary>
        [Fact]
        public void Der_Altbestand_bekommt_den_Halbgeviertstrich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            IReadOnlyList<Katalogfilterzeile> zeilen = KlimaregionStammCtrl.Katalogfilterzeilen();

            // Die Testdatenbank fuehrt den Bestand - alle Regionen ohne Herkunft.
            Assert.NotEmpty(zeilen);
            Assert.All(zeilen, z => Assert.Equal(ParameterVerwendung.LEER,
                                                 z.Text(Katalogfilterprofil.SpQuelle)));
        }

        /// <summary>
        /// <b>Ohne die zwei Spalten des Schrittes 97 steht die Liste trotzdem</b>, und
        /// sie zeigt die QUELLE weiter: Eine Datenbank auf Stand 95 ist kein Grund, die
        /// Spalte leer zu lassen (Muster <c>KostenVorlagenCtrl.PflichtSpalteVorhanden</c>).
        /// </summary>
        [Fact]
        public void Ohne_Schritt_97_zeigt_die_Liste_weiter_die_Quelle()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            RegionAnlegen("KL6 Stand 95", DbWerte.KLIMA_QUELLE_PVGIS,
                          DbWerte.KLIMA_SZENARIO_MITTEL, 2015);

            DataRepository.ExecuteNonQuery(
                "ALTER TABLE Tab_Klimaregion_STAMM DROP COLUMN " + SchemaKatalog.SPALTE_KR_SZENARIO);
            DataRepository.ExecuteNonQuery(
                "ALTER TABLE Tab_Klimaregion_STAMM DROP COLUMN " + SchemaKatalog.SPALTE_KR_BEZUGSJAHR);

            Assert.False(DataRepository.SpalteVorhanden(
                KlimaregionStammCtrl.TAB_REGION_STAMM, SchemaKatalog.SPALTE_KR_SZENARIO));

            IReadOnlyList<Katalogfilterzeile> zeilen = KlimaregionStammCtrl.Katalogfilterzeilen();

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_PVGIS,
                         zeilen.Single(z => z.Bezeichner == "KL6 Stand 95")
                               .Text(Katalogfilterprofil.SpQuelle));
        }

        // =====================================================================
        //  5 — Die Startseite (A-KL6-2)
        // =====================================================================

        /// <summary>
        /// <b>Die Herkunft des Projekts nennt Szenario und Bezugsjahr</b> (A-KL6-2):
        /// Der Kern liefert die SCHLÜSSEL und die Zahl, den Satz daraus baut
        /// <see cref="KlimaAnzeige.Quellenzeile"/> — dieselbe Stelle wie für die Liste.
        /// </summary>
        [Fact]
        public void Die_Herkunft_des_Projekts_nennt_Szenario_und_Jahr()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            int stammId = RegionAnlegen("KL6 Startseite", DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                                        DbWerte.KLIMA_SZENARIO_WINTERKALT, 2015);

            Assert.Equal(KlimaStand.Gespeichert,
                         StartseiteCtrl.KlimaregionSpeichern(PROJEKT, NAME_PROJEKT, stammId));

            KlimaHerkunft h = StartseiteCtrl.KlimaHerkunft(PROJEKT);

            Assert.NotNull(h);
            Assert.Equal(DbWerte.KLIMA_QUELLE_TRY_REGIONAL, h.Quelle);
            Assert.Equal(DbWerte.KLIMA_SZENARIO_WINTERKALT, h.Szenario);
            Assert.Equal(2015, h.Bezugsjahr);

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_TRY_REGIONAL + " · 2015 · " +
                         WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_SZ_WINTERKALT,
                         KlimaAnzeige.Quellenzeile(h.Quelle, h.Szenario, h.Bezugsjahr));
        }

        /// <summary>
        /// <b>Der Eintrag des Auswahlfeldes</b> (A-KL6-2): „&lt;Name&gt; (TRY 2045
        /// sommerwarm)". Eine Region ohne Herkunft behält ihren blanken Namen — die
        /// Klammer wird nicht erfunden.
        /// </summary>
        [Fact]
        public void Der_Eintrag_des_Auswahlfeldes_nennt_das_Wetterjahr()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            RegionAnlegen("KL6 Auswahl", DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                          DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045);

            IReadOnlyList<(int Id, string Name)> eintraege = StartseiteCtrl.KlimaregionenMitId();

            string erwartet = "KL6 Auswahl (" + WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_KURZ_TRY +
                              " 2045 " + WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM + ")";

            Assert.Contains(eintraege, e => e.Name == erwartet);

            // Eine Bestandsregion behaelt ihren blanken Namen.
            Assert.Contains(eintraege, e => e.Name == "München");
        }

        // =====================================================================
        //  6 — Der Satzbau, in beiden Kulturen
        // =====================================================================

        /// <summary>
        /// <b>Der Satz steht in beiden Sprachen</b> — und der Schlüssel in der
        /// Datenbank ändert sich dabei nicht. Geprüft wird gegen die RESSOURCE, nicht
        /// gegen ein abgeschriebenes Literal: Was dort steht, ist die eine Wahrheit.
        /// </summary>
        [Theory]
        [InlineData("de-DE")]
        [InlineData("en-US")]
        public void Der_Satzbau_steht_in_beiden_Kulturen(string kultur)
        {
            using var k = new Kulturvorrichtung(kultur);

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_TRY_REGIONAL + " · 2045 · " +
                         WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM,
                         KlimaAnzeige.Quellenzeile(DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                                                   DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045));

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_KURZ_TRY + " 2045 " +
                         WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM,
                         KlimaAnzeige.Kurzform(DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                                               DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045));

            Assert.Equal("hagelloch (" + WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_KURZ_TRY + " 2045 " +
                         WindowsFormsApplication1.MyResource.Resource.KLIMA_TRY_SZ_SOMMERWARM + ")",
                         KlimaAnzeige.Eintrag("hagelloch", DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                                              DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045));

            Assert.Equal("München (" + WindowsFormsApplication1.MyResource.Resource.KLIMA_QUELLE_KURZ_PVGIS + ")",
                         KlimaAnzeige.Eintrag("München", DbWerte.KLIMA_QUELLE_PVGIS, null, null));
        }

        /// <summary>
        /// <b>Was fehlt, wird weggelassen — nie ersetzt.</b> Ohne jede Angabe bleibt die
        /// leere Zeichenfolge; den Halbgeviertstrich macht erst die Katalogliste daraus.
        /// Das Bezugsjahr steht OHNE Tausendertrennung da — eine Jahreszahl ist kein
        /// Betrag.
        /// </summary>
        [Fact]
        public void Was_fehlt_wird_weggelassen()
        {
            Assert.Equal("", KlimaAnzeige.Quellenzeile("", "", null));
            Assert.Equal("", KlimaAnzeige.Quellenzeile(null, null, 0));
            Assert.Equal("", KlimaAnzeige.Kurzform("", "", null));
            Assert.Equal("Berlin", KlimaAnzeige.Eintrag("Berlin", "", "", null));

            Assert.Equal("2045", KlimaAnzeige.Jahrtext(2045));
            Assert.Equal("", KlimaAnzeige.Jahrtext(null));
            Assert.Equal("", KlimaAnzeige.Jahrtext(0));

            // Ein unbekannter Schluessel wird nicht geraten.
            Assert.Equal("", KlimaAnzeige.Szenariotext("SOMMERKALT"));
            Assert.Equal("", KlimaAnzeige.Quellentext("ERDFUNK"));
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>
        /// Legt eine Region mit Herkunft an und gibt ihre Stamm-Id zurück — über
        /// <c>KlimaregionStammCtrl.Add</c>, also über denselben Weg, den der Import
        /// geht.
        /// </summary>
        private static int RegionAnlegen(string name, string quelle, string szenario, int? jahr)
        {
            var ctrl = new KlimaregionStammCtrl();

            using (DbVorgang v = DataRepository.Vorgang())
            {
                ctrl.Add(name, 9.1829, 48.7758, "Stuttgart", quelle, "2026-09-19", szenario, jahr, v);
                v.Commit();
            }

            return KlimaregionStammCtrl.IdVonName(name);
        }

        /// <summary>
        /// Eine vollständige, SYNTHETISCHE TRY-Datei (34 Kopfzeilen, 8 760 Datenzeilen)
        /// im Temp-Ordner — Bauart wie in <c>KlimaspaltenTests</c>, nur dass die ERSTEN
        /// Kopfzeilen hier gesetzt werden können. Keine DWD-Originaldaten.
        /// </summary>
        /// <param name="kopfzeilen">Der Inhalt der ersten Kopfzeilen; der Rest bleibt
        /// nichtssagend. Nichts oder <c>null</c> = ein Kopf ohne jede Angabe.</param>
        private static string TryDatei(params string[] kopfzeilen)
        {
            string pfad = Path.Combine(Path.GetTempPath(),
                "epos_kl6_" + Guid.NewGuid().ToString("N") + ".dat");

            string[] gesetzt = kopfzeilen ?? Array.Empty<string>();

            var sb = new StringBuilder(700 * 1024);
            for (int i = 1; i <= 34; i++)
            {
                string zeile = i <= gesetzt.Length ? gesetzt[i - 1] : null;
                sb.Append(string.IsNullOrEmpty(zeile)
                          ? "Kopfzeile " + i.ToString(CultureInfo.InvariantCulture)
                          : zeile).Append("\r\n");
            }
            sb.Append("*** \r\n");

            int[] tageMonat = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            for (int m = 1; m <= 12; m++)
                for (int tag = 1; tag <= tageMonat[m - 1]; tag++)
                    for (int h = 1; h <= 24; h++)
                    {
                        int b = h >= 9 && h <= 16 ? 150 : 0;
                        int diff = h >= 9 && h <= 16 ? 60 : 0;
                        sb.AppendFormat(CultureInfo.InvariantCulture,
                            "4321000 5678000 {0} {1} {2} {3} 1013 180 2.0 4 3.0 80 {4} {5} 260 300 1\r\n",
                            m, tag, h, 8.5, b, diff);
                    }

            File.WriteAllText(pfad, sb.ToString());
            return pfad;
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static int Zahl(string sql, int id)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@id", id));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string spalte, int stammId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT " + spalte + " FROM " + SchemaKatalog.TAB_KLIMAREGION_STAMM +
                " WHERE ID_Klimaregion = ?", new DbParam("@id", stammId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }

        private static string ProjektText(string spalte, int projektRegionId)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT " + spalte + " FROM " + SchemaKatalog.TAB_KLIMAREGION + " WHERE ID = ?",
                new DbParam("@id", projektRegionId));
            return o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);
        }
    }
}
