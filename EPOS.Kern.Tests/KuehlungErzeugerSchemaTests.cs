using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Schemaschritt der Kühlung am Erzeuger — <b>114 (KU-S3)</b>, Stufe KU2 Welle 1
    /// (Kühlkonzept 7.3; Entscheide E15 und E33).
    ///
    /// <para><b>Geprüft wird:</b> die Definitionen gegen das Papier (sechs Spalten an den zwei
    /// Wärmepumpentabellen, die Stromträgerwahl der Kühlung an der Anlagenzeile mit Beziehung auf
    /// <c>energy_carrier.id</c>); der Stand der Testdatenbank (alles steht, alles 0 bzw. NULL,
    /// STRICT bleibt); der Schritt aus dem Stand davor und wiederholbar; die Leser und Schreiber
    /// NULL-erhaltend — Projektkopie, Katalogkopie, Übernahme in den Katalog, Katalogpflege,
    /// Anlagenzeile samt Speicherweg Löschen + Neuanlegen, Konfigurationsschreibweg, Duplizieren,
    /// die Laststufe der Kühlkennlinie; und dass die Beziehung ihren Träger schützt.</para>
    ///
    /// <para><b>Eigene Arbeitskopie je Fall</b> — fast jeder Fall schreibt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KuehlungErzeugerSchemaTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Das Referenzprojekt mit Kühlung und seine Wärmepumpe (Projektkopie, Anlagenzeile).</summary>
        private const int PROJEKT = 1017, WP_KOPIE = 1017033, ANLAGE = 10211;

        /// <summary>Ein Katalogsatz mit Kühlkennlinie (60 Stützstellen, Laststufen gepflegt).</summary>
        private const int STAMM_MIT_KUEHLKENNLINIE = 42;

        /// <summary>Ein Stromträger des Katalogs (<c>pricing_model = 'ELECTRICITY'</c>).</summary>
        private const int STROMTRAEGER = 60;

        /// <summary>
        /// Seit KU2 Welle 2 lässt sich der Kühlbetrieb nur mit einer Kühlkennlinie IM PROJEKT setzen
        /// (Sperrgrund, Kühlkonzept 5.0.1, 10.3). Die Fälle, die den Kühlbetrieb einschalten, säen
        /// deshalb eine Phantasie-Kennlinie für die Projektkopie — eine Stützstelle mit runden Werten,
        /// kein Produkt.
        /// </summary>
        private static void KennlinieSaeen()
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Kenndaten_Kuehlung (ID, ID_WP, Vorlauf, Temperatur, COP, Pkuehl, [Last]) " +
                "VALUES ((SELECT COALESCE(MAX(ID), 0) + 1 FROM Tab_Kenndaten_Kuehlung), ?, 18, 30, 4.0, 10.0, 100)",
                new DbParam("@wp", WP_KOPIE)));
        }

        // =============================================================================
        //  Teil 1 - Definitionen (ohne Datenbank)
        // =============================================================================

        [Fact]
        public void Der_Zielstand_ist_mindestens_114()
        {
            Assert.True(SchemaStand.Zielversion >= 114,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 114.");
        }

        /// <summary>
        /// KU-S3 nach Kühlkonzept 7.3: je drei Spalten an <c>Tab_WP</c> und <c>Tab_WP_STAMM</c>
        /// — der Schalter 0/1 mit Vorgabe 0, der Vorlauf als <c>INTEGER</c> wie die Stützstellen,
        /// der Hilfsstromanteil als <c>REAL</c>; beide nullbar, ohne DDL-Vorgabe.
        /// </summary>
        [Fact]
        public void KU_S3_sechs_Eintraege_drei_je_Waermepumpentabelle_mit_den_Typen_aus_7_3()
        {
            Assert.Equal(6, KuehlungSchema.Erzeugerspalten.Length);
            foreach (string t in new[] { "Tab_WP", "Tab_WP_STAMM" })
                Assert.Equal(new[] { "Kuehlbetrieb", "Kuehl_Vorlauf", "Kuehl_Hilfsstromanteil" },
                             KuehlungSchema.Erzeugerspalten.Where(s => s.Tabelle == t).Select(s => s.Name).ToArray());
            Assert.Equal(WPStammCtrl.TABLE, KuehlungSchema.TAB_WP_STAMM);

            foreach (SchemaSpalte s in KuehlungSchema.Erzeugerspalten)
            {
                Assert.True(s.Name.All(c => c < 128), s.Name + " ist nicht ASCII.");
                string typ = StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition);
                switch (s.Name)
                {
                    case "Kuehlbetrieb":
                        Assert.Equal("INTEGER NOT NULL DEFAULT 0 CHECK (\"Kuehlbetrieb\" IN (0,1))", typ);
                        break;
                    case "Kuehl_Vorlauf":
                        Assert.Equal("INTEGER", typ);
                        break;
                    default:
                        Assert.Equal("REAL", typ);
                        break;
                }
            }
        }

        /// <summary>
        /// Die Stromträgerwahl der Kühlung (K9, E33) steht an der Anlagenzeile — ein Verweis mit
        /// Beziehung auf <c>energy_carrier.id</c>, <c>ON DELETE SET NULL</c>, ohne Vorgabe (NULL =
        /// wie Heizbetrieb). Keine der sieben Spalten gehört zur Rückfallebene der Eingabeseite.
        /// </summary>
        [Fact]
        public void KU_S3_Stromtraegerwahl_an_der_Anlagenzeile_mit_Beziehung_und_ohne_Rueckfallebene()
        {
            Assert.Equal("Kuehl_ID_Carrier", KuehlungSchema.SPALTE_KUEHL_ID_CARRIER);
            Assert.Equal("INTEGER REFERENCES \"energy_carrier\" (\"id\") ON DELETE SET NULL",
                         KuehlungSchema.TYP_KUEHL_ID_CARRIER);
            Assert.DoesNotContain("DEFAULT", KuehlungSchema.TYP_KUEHL_ID_CARRIER, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith("ALTER TABLE \"Tab_Energieanlagen\" ADD COLUMN \"Kuehl_ID_Carrier\" ",
                              KuehlungSchema.SQL_KUEHL_ID_CARRIER, StringComparison.Ordinal);

            var rueckfall = new HashSet<string>(SchemaKatalog.Alle.Select(s => s.Tabelle + "." + s.Name),
                                                StringComparer.OrdinalIgnoreCase);
            foreach (SchemaSpalte s in KuehlungSchema.Erzeugerspalten)
                Assert.DoesNotContain(s.Tabelle + "." + s.Name, rueckfall);
            Assert.DoesNotContain("Tab_Energieanlagen.Kuehl_ID_Carrier", rueckfall);
        }

        /// <summary>
        /// Die Stromträgerwahl ist eine MODELLspalte wie <c>ID_Carrier</c>: Die eine
        /// Einfügeanweisung der Anlagenzeile nennt sie, und Platzhalter und Parameter bleiben
        /// gleich viele.
        /// </summary>
        [Fact]
        public void Die_Einfuegeanweisung_der_Anlagenzeile_nennt_den_Kuehltraeger()
        {
            Assert.Contains("Kuehl_ID_Carrier", AnlagenSql.SQL_ANLAGE_INSERT, StringComparison.Ordinal);
            int platzhalter = AnlagenSql.SQL_ANLAGE_INSERT.Count(c => c == '?');
            Assert.Equal(65, platzhalter);
            Assert.Equal(platzhalter, AnlagenSql.AnlagenParameter(1, new WErzeugerModel()).Length);
        }

        // =============================================================================
        //  Teil 2 - die Testdatenbank auf Stand 114
        // =============================================================================

        /// <summary>
        /// Alle sieben Spalten stehen, jede Wärmepumpe steht auf „kein Kühlbetrieb", Vorlauf,
        /// Hilfsstromanteil und Stromträgerwahl sind NULL; die Beziehung steht mit
        /// <c>SET NULL</c>, der Schalter nimmt nur 0 und 1 an, und die Tabellen bleiben STRICT.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_steht_auf_114_und_alles_ist_aus_bzw_leer()
        {
            if (!_db.Vorhanden) return;

            Assert.True(Zahl("SELECT SchemaVersion FROM Tab_Applikation") >= 114);
            Assert.True(KuehlungSchema.ErzeugerspaltenVollstaendig());

            foreach (string t in new[] { "Tab_WP", "Tab_WP_STAMM" })
            {
                Assert.True(Zahl("SELECT COUNT(*) FROM [" + t + "]") > 0);
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + t + "] WHERE Kuehlbetrieb <> 0"));
                Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM [" + t + "] WHERE Kuehl_Vorlauf IS NOT NULL " +
                                      "OR Kuehl_Hilfsstromanteil IS NOT NULL"));
            }
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Kuehl_ID_Carrier IS NOT NULL"));

            DataTable fk = DataRepository.GetDataTable(
                "SELECT \"table\" AS ziel, \"to\" AS zielspalte, on_delete FROM pragma_foreign_key_list('Tab_Energieanlagen') " +
                "WHERE \"from\" = 'Kuehl_ID_Carrier'");
            DataRow b = Assert.Single(fk.Rows.Cast<DataRow>());
            Assert.Equal("energy_carrier", Convert.ToString(b["ziel"], CultureInfo.InvariantCulture));
            Assert.Equal("id", Convert.ToString(b["zielspalte"], CultureInfo.InvariantCulture));
            Assert.Equal("SET NULL", Convert.ToString(b["on_delete"], CultureInfo.InvariantCulture));

            // Die Pruefung der Spalte weist eine 2 ab - der Zugriff meldet den Fehler, der Wert bleibt.
            try
            {
                DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_WP SET Kuehlbetrieb = 2 WHERE ID = " + WP_KOPIE.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception) { }
            Assert.Equal(0L, Zahl("SELECT Kuehlbetrieb FROM Tab_WP WHERE ID = " + WP_KOPIE.ToString(CultureInfo.InvariantCulture)));

            foreach (string t in new[] { "Tab_WP", "Tab_WP_STAMM", "Tab_Energieanlagen" })
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?", new DbParam("?", t)),
                    CultureInfo.InvariantCulture);
                Assert.EndsWith("STRICT", ddl.TrimEnd(), StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// KU-S3 aus dem Stand VOR ihm: Die sechs Spalten am Gerät werden entfernt, der Schritt legt
        /// sie wieder an (die Stromträgerwahl steht und bleibt — eine Spalte mit Beziehung lässt
        /// SQLite nicht fallen); die Bestandswerte bleiben, und ein zweiter Lauf legt nichts an.
        /// </summary>
        [Fact]
        public void KU_S3_aus_dem_Stand_davor_und_wiederholbar()
        {
            if (!_db.Vorhanden) return;

            string bestand = Abdruck("SELECT ID, Bezeichner, Nennleistung, Kuehlleistung FROM Tab_WP ORDER BY ID") +
                             Abdruck("SELECT ID, Bezeichner, Nennleistung, Kuehlleistung FROM Tab_WP_STAMM ORDER BY ID");

            foreach (SchemaSpalte s in KuehlungSchema.Erzeugerspalten)
                DataRepository.ExecuteNonQuery("ALTER TABLE \"" + s.Tabelle + "\" DROP COLUMN \"" + s.Name + "\"");
            Assert.False(KuehlungSchema.ErzeugerspaltenVollstaendig());

            var bericht = new List<string>();
            Assert.Equal(6, KuehlungSchema.ErzeugerspaltenAlle(bericht));
            Assert.Contains(bericht, z => z.Contains("6 von 6 Spalte(n)"));
            Assert.Contains(bericht, z => z.Contains("Kuehl_ID_Carrier: vorhanden"));
            Assert.True(KuehlungSchema.ErzeugerspaltenVollstaendig());
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_WP WHERE Kuehlbetrieb <> 0"));
            Assert.Equal(bestand,
                         Abdruck("SELECT ID, Bezeichner, Nennleistung, Kuehlleistung FROM Tab_WP ORDER BY ID") +
                         Abdruck("SELECT ID, Bezeichner, Nennleistung, Kuehlleistung FROM Tab_WP_STAMM ORDER BY ID"));

            Assert.Equal(0, KuehlungSchema.ErzeugerspaltenAlle(null));
            Assert.True(KuehlungSchema.ErzeugerspaltenVollstaendig());
        }

        // =============================================================================
        //  Teil 3 - Gerät: Projektkopie, Katalog, Übernahme, NULL-erhaltend
        // =============================================================================

        /// <summary>
        /// DIE KATALOGKOPIE TRÄGT DIE KÜHLKONFIGURATION: Ein Katalogsatz mit Kühlbetrieb und
        /// Kühl-Vorlauf und leerem Hilfsstromanteil kommt so im Projekt an — NULL bleibt NULL —,
        /// und die Kühlkennlinie reist samt Laststufen mit.
        /// </summary>
        [Fact]
        public void Die_Katalogkopie_traegt_die_Kuehlkonfiguration_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_WP_STAMM SET Kuehlbetrieb = 1, Kuehl_Vorlauf = 18 WHERE ID = ?",
                new DbParam("?", STAMM_MIT_KUEHLKENNLINIE));

            int neu = new WPCtrl().CopyFromStamm(STAMM_MIT_KUEHLKENNLINIE, PROJEKT);
            Assert.True(neu > 0, "Die Katalogkopie ist gescheitert.");

            DataRow z = Zeile("SELECT * FROM Tab_WP WHERE ID = " + neu.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(z["Kuehlbetrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(18L, Convert.ToInt64(z["Kuehl_Vorlauf"], CultureInfo.InvariantCulture));
            Assert.True(z["Kuehl_Hilfsstromanteil"] == DBNull.Value, "Der Hilfsstromanteil ist in der Kopie nicht NULL.");

            string stamm = STAMM_MIT_KUEHLKENNLINIE.ToString(CultureInfo.InvariantCulture);
            string kopie = neu.ToString(CultureInfo.InvariantCulture);
            Assert.Equal(Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung_STAMM WHERE ID_WP = " + stamm),
                         Zahl("SELECT COUNT(*) FROM Tab_Kenndaten_Kuehlung WHERE ID_WP = " + kopie));
            Assert.Equal(Abdruck("SELECT Vorlauf, Temperatur, COP, Pkuehl, [Last] FROM Tab_Kenndaten_Kuehlung_STAMM " +
                                 "WHERE ID_WP = " + stamm + " ORDER BY ID"),
                         Abdruck("SELECT Vorlauf, Temperatur, COP, Pkuehl, [Last] FROM Tab_Kenndaten_Kuehlung " +
                                 "WHERE ID_WP = " + kopie + " ORDER BY ID"));

            // Und der Leser der Projektkopie sieht dasselbe.
            var leser = new WPCtrl();
            leser.ReadSingle("SELECT * FROM Tab_WP WHERE ID = " + kopie);
            Assert.True(leser.Kuehlbetrieb);
            Assert.Equal(18, leser.KuehlVorlauf);
            Assert.Null(leser.KuehlHilfsstromanteil);
        }

        /// <summary>
        /// DER SCHREIBWEG DER PROJEKTKOPIE hält NULL: <c>Insert</c> schreibt die drei Felder des
        /// Modells, wie sie sind, und die Liste liest sie so zurück — auch den leeren Vorlauf.
        /// </summary>
        [Fact]
        public void Insert_und_Lesen_der_Projektkopie_halten_die_Kuehlfelder_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            var wp = new WPCtrl
            {
                WPName = "KU-S3-Probe Projektkopie",
                ID_Projekt = PROJEKT,
                Kuehlbetrieb = true,
                KuehlVorlauf = null,
                KuehlHilfsstromanteil = 0.05,
            };
            Assert.True(wp.Insert());
            Assert.True(wp.ID > 0);

            var leser = new WPCtrl();
            leser.ReadAll("ID = " + wp.ID.ToString(CultureInfo.InvariantCulture));
            WPModel m = Assert.Single(leser.items);
            Assert.True(m.Kuehlbetrieb);
            Assert.Null(m.KuehlVorlauf);
            Assert.Equal(0.05, m.KuehlHilfsstromanteil);
        }

        /// <summary>
        /// DER SCHREIBWEG DER KÜHLKONFIGURATION: <c>null</c> heißt hier NULL, nicht „unverändert"
        /// — Vorlauf und Hilfsstromanteil lassen sich leeren. Ein Hilfsstromanteil außerhalb
        /// 0 ≤ x &lt; 1 wird abgelehnt, und nichts ist geschrieben; ein fremdes Gerät ebenso.
        /// </summary>
        [Fact]
        public void Die_Kuehlkonfiguration_schreibt_NULL_als_NULL_und_prueft_den_Hilfsstromanteil()
        {
            if (!_db.Vorhanden) return;

            KennlinieSaeen();
            WPCtrl.SpeicherErgebnis e = WPCtrl.KuehlkonfigurationSchreiben(WP_KOPIE, PROJEKT, true, 18, 0.1);
            Assert.True(e.Ok, e.Meldung);
            DataRow z = Zeile("SELECT Kuehlbetrieb, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil FROM Tab_WP WHERE ID = " +
                              WP_KOPIE.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(z["Kuehlbetrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(18L, Convert.ToInt64(z["Kuehl_Vorlauf"], CultureInfo.InvariantCulture));
            Assert.Equal(0.1, Convert.ToDouble(z["Kuehl_Hilfsstromanteil"], CultureInfo.InvariantCulture));

            foreach (double falsch in new[] { -0.01, 1.0, 1.5, double.NaN })
                Assert.False(WPCtrl.KuehlkonfigurationSchreiben(WP_KOPIE, PROJEKT, false, null, falsch).Ok);
            Assert.False(WPCtrl.KuehlkonfigurationSchreiben(WP_KOPIE, PROJEKT + 1, false, null, null).Ok);
            z = Zeile("SELECT Kuehlbetrieb, Kuehl_Vorlauf FROM Tab_WP WHERE ID = " +
                      WP_KOPIE.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(1L, Convert.ToInt64(z["Kuehlbetrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(18L, Convert.ToInt64(z["Kuehl_Vorlauf"], CultureInfo.InvariantCulture));

            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP_KOPIE, PROJEKT, false, null, null).Ok);
            z = Zeile("SELECT Kuehlbetrieb, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil FROM Tab_WP WHERE ID = " +
                      WP_KOPIE.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(0L, Convert.ToInt64(z["Kuehlbetrieb"], CultureInfo.InvariantCulture));
            Assert.True(z["Kuehl_Vorlauf"] == DBNull.Value, "Der Vorlauf ist nicht NULL.");
            Assert.True(z["Kuehl_Hilfsstromanteil"] == DBNull.Value, "Der Hilfsstromanteil ist nicht NULL.");
        }

        /// <summary>
        /// DIE ÜBERNAHME IN DEN KATALOG trägt die drei Felder mit — dieselben Namen in Projektkopie
        /// und Katalog —, und der Katalogleser liest sie NULL-erhaltend zurück.
        /// </summary>
        [Fact]
        public void Die_Uebernahme_in_den_Katalog_traegt_die_Kuehlkonfiguration()
        {
            if (!_db.Vorhanden) return;

            KennlinieSaeen();
            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP_KOPIE, PROJEKT, true, 7, null).Ok);
            WPStammCtrl.SpeicherErgebnis e = WPStammCtrl.UebernehmenAusProjekt(WP_KOPIE, PROJEKT, false);
            Assert.True(e.Ok, e.Meldung);

            long stamm = Zahl("SELECT ID_Stamm FROM Tab_WP WHERE ID = " + WP_KOPIE.ToString(CultureInfo.InvariantCulture));
            var katalog = new WPStammCtrl();
            katalog.ReadAll("ID = " + stamm.ToString(CultureInfo.InvariantCulture));
            WPModel m = Assert.Single(katalog.items);
            Assert.True(m.Kuehlbetrieb);
            Assert.Equal(7, m.KuehlVorlauf);
            Assert.Null(m.KuehlHilfsstromanteil);
        }

        /// <summary>
        /// DIE KATALOGPFLEGE: <c>WPStammCtrl.Insert</c> schreibt die drei Felder des Modells, wie
        /// sie sind; ein frisches Modell steht auf „aus" und leer.
        /// </summary>
        [Fact]
        public void Insert_und_Lesen_des_Katalogs_halten_die_Kuehlfelder_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            var frisch = new WPStammCtrl { WPName = "KU-S3-Probe Katalog leer" };
            Assert.False(frisch.Kuehlbetrieb);
            Assert.Null(frisch.KuehlVorlauf);
            Assert.Null(frisch.KuehlHilfsstromanteil);
            Assert.True(frisch.Insert());

            var gesetzt = new WPStammCtrl
            {
                WPName = "KU-S3-Probe Katalog gesetzt",
                Kuehlbetrieb = true,
                KuehlVorlauf = 7,
                KuehlHilfsstromanteil = 0.08,
            };
            Assert.True(gesetzt.Insert());

            var leser = new WPStammCtrl();
            leser.ReadAll("Bezeichner LIKE 'KU-S3-Probe Katalog%'");
            Assert.Equal(2, leser.items.Count);
            WPModel leer = leser.items.Single(x => x.WPName.EndsWith("leer", StringComparison.Ordinal));
            Assert.False(leer.Kuehlbetrieb);
            Assert.Null(leer.KuehlVorlauf);
            Assert.Null(leer.KuehlHilfsstromanteil);
            WPModel voll = leser.items.Single(x => x.WPName.EndsWith("gesetzt", StringComparison.Ordinal));
            Assert.True(voll.Kuehlbetrieb);
            Assert.Equal(7, voll.KuehlVorlauf);
            Assert.Equal(0.08, voll.KuehlHilfsstromanteil);
        }

        /// <summary>
        /// DIE PARAMETERÜBERSICHT des Katalogs führt die drei Spalten als „nicht verwendet" — noch
        /// liest sie kein Rechenweg — und zeigt den Schalter als Ja/Nein, die leeren Werte als Strich.
        /// </summary>
        [Fact]
        public void Die_Parameteruebersicht_fuehrt_die_drei_Spalten_als_nicht_verwendet()
        {
            if (!_db.Vorhanden) return;

            foreach (string spalte in new[] { "Kuehlbetrieb", "Kuehl_Vorlauf", "Kuehl_Hilfsstromanteil" })
            {
                ParameterEintrag e = ParameterVerwendung.Katalog(Anlagenart.Waermepumpe).Single(x => x.Spalte == spalte);
                Assert.False(e.Gerechnet);
            }

            string bezeichner = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_WP_STAMM WHERE ID = ?", new DbParam("?", STAMM_MIT_KUEHLKENNLINIE)),
                CultureInfo.InvariantCulture);
            IReadOnlyList<Parameterwert> werte = ParameterUebersichtCtrl.Werte(Anlagenart.Waermepumpe, bezeichner);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ALLG_BTN_NEIN, werte.Single(w => w.Eintrag.Spalte == "Kuehlbetrieb").Wert);
            Assert.Equal(ParameterVerwendung.LEER, werte.Single(w => w.Eintrag.Spalte == "Kuehl_Vorlauf").Wert);
            Assert.Equal(ParameterVerwendung.LEER, werte.Single(w => w.Eintrag.Spalte == "Kuehl_Hilfsstromanteil").Wert);
        }

        // =============================================================================
        //  Teil 4 - Anlagenzeile: der Stromträger der Kühlung
        // =============================================================================

        /// <summary>
        /// DER KONFIGURATIONSSCHREIBWEG: Ein gewählter Träger wird geschrieben; ein Speichern
        /// anderer Felder lässt ihn stehen (<c>null</c> = nicht anfassen); 0 setzt „wie
        /// Heizbetrieb" (NULL, nie 0); ein Träger, den es nicht gibt, fällt auf NULL. Der Leser
        /// der Anlagenzeile liefert jeden Stand NULL-erhaltend.
        /// </summary>
        [Fact]
        public void Der_Konfigurationsschreibweg_fuehrt_den_Kuehltraeger_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            Assert.Null(Anlage().Kuehl_ID_Carrier);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: STROMTRAEGER)).Ok);
            Assert.Equal(STROMTRAEGER, Anlage().Kuehl_ID_Carrier);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(Heizstab: true)).Ok);
            Assert.Equal(STROMTRAEGER, Anlage().Kuehl_ID_Carrier);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: 0)).Ok);
            Assert.Null(Anlage().Kuehl_ID_Carrier);
            Assert.True(Zeile("SELECT Kuehl_ID_Carrier FROM Tab_Energieanlagen WHERE ID = " +
                              ANLAGE.ToString(CultureInfo.InvariantCulture))[0] == DBNull.Value);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: 987654)).Ok);
            Assert.Null(Anlage().Kuehl_ID_Carrier);
        }

        /// <summary>
        /// DER SPEICHERWEG LÖSCHEN + NEUANLEGEN (<c>WizardCtrl.Del_Projekt_Waermeerzeuger</c> und
        /// <c>Add_WP_Waermeerzeuger</c>, Zeichen für Zeichen): Die eine Einfügeanweisung der
        /// Anlagenzeile trägt den Kühlträger aus dem Modell mit — er ist keine Fachspalte, die
        /// gerettet werden müsste —, und ein Verweis ins Leere fällt auf NULL, statt das Einfügen
        /// nach dem Löschen scheitern zu lassen.
        /// </summary>
        [Fact]
        public void Der_Speicherweg_traegt_den_Kuehltraeger_im_Modell()
        {
            if (!_db.Vorhanden) return;

            Assert.DoesNotContain(KuehlungSchema.SPALTE_KUEHL_ID_CARRIER, WizardCtrl.Fachspalten(),
                                  StringComparer.OrdinalIgnoreCase);

            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: STROMTRAEGER)).Ok);
            WErzeugerModel m = Anlage();
            Assert.Equal(STROMTRAEGER, m.Kuehl_ID_Carrier);

            Speichern(m);
            Assert.Equal((long)STROMTRAEGER, Zahl(KUEHLTRAEGER_DER_WAERMEPUMPE));

            m.Kuehl_ID_Carrier = 987654;
            Speichern(m);
            Assert.True(Leer(DataRepository.ExecuteScalar(KUEHLTRAEGER_DER_WAERMEPUMPE)), "Der Kuehltraeger ist nicht NULL.");

            m.Kuehl_ID_Carrier = null;
            Speichern(m);
            Assert.True(Leer(DataRepository.ExecuteScalar(KUEHLTRAEGER_DER_WAERMEPUMPE)), "Der Kuehltraeger ist nicht NULL.");
        }

        /// <summary>Der Kühlträger der (einen) Wärmepumpenanlage von 1017.</summary>
        private const string KUEHLTRAEGER_DER_WAERMEPUMPE =
            "SELECT Kuehl_ID_Carrier FROM Tab_Energieanlagen WHERE ID_Projekt = 1017 AND ID_Type = 1";

        /// <summary>Der Speicherweg des Assistenten für die Wärmepumpen eines Projekts.</summary>
        private static void Speichern(WErzeugerModel m)
        {
            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.WP_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, new List<WErzeugerModel> { m }));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = 1017 AND ID_Type = 1"));
        }

        /// <summary>
        /// DIE BEZIEHUNG SCHÜTZT IHREN TRÄGER: Ein Träger, den eine Anlage allein als Kühlträger
        /// führt, lässt sich weder aus dem Katalog löschen noch aus dem Projekt lösen; wird er doch
        /// gelöscht, fällt die Anlage auf „wie Heizbetrieb" zurück (<c>ON DELETE SET NULL</c>).
        /// </summary>
        [Fact]
        public void Die_Beziehung_schuetzt_den_Kuehltraeger_und_faellt_auf_NULL_zurueck()
        {
            if (!_db.Vorhanden) return;

            long neu = Zahl("SELECT MAX(id) + 1 FROM energy_carrier");
            DataRepository.ExecuteNonQuery(
                "INSERT INTO energy_carrier (id, name, pricing_model, is_active) VALUES (?, ?, 'ELECTRICITY', 1)",
                new DbParam("?", neu), new DbParam("?", "KU-S3-Probe Kuehlstrom"));
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: (int)neu)).Ok);
            Assert.Equal((int)neu, Anlage().Kuehl_ID_Carrier);

            Assert.False(EnergietraegerKatalogCtrl.Loeschen((int)neu, out string grund));
            Assert.Contains("1 Anlage(n)", grund);
            Assert.False(EnergietraegerKatalogCtrl.AusProjektEntfernen(PROJEKT, (int)neu, out grund));
            Assert.Contains("1 Anlage(n)", grund);

            DataRepository.ExecuteNonQuery("DELETE FROM energy_carrier WHERE id = ?", new DbParam("?", neu));
            Assert.Null(Anlage().Kuehl_ID_Carrier);
        }

        /// <summary>
        /// DAS DUPLIZIEREN trägt Kühlkonfiguration und Kühlträger mit — der Katalog der Träger
        /// wird nicht kopiert, der Verweis zeigt weiter auf denselben Träger.
        /// </summary>
        [Fact]
        public void Das_Duplizieren_traegt_Kuehlkonfiguration_und_Kuehltraeger_mit()
        {
            if (!_db.Vorhanden) return;

            KennlinieSaeen();
            Assert.True(WPCtrl.KuehlkonfigurationSchreiben(WP_KOPIE, PROJEKT, true, 18, 0.07).Ok);
            Assert.True(WErzeugerCtrl.KonfigurationSchreiben(ANLAGE, PROJEKT,
                new WErzeugerCtrl.KonfigurationFelder(KuehlIdCarrier: STROMTRAEGER)).Ok);
            string quelle = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Projektname FROM Tab_Projekt WHERE ID = ?", new DbParam("?", PROJEKT)),
                CultureInfo.InvariantCulture);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(quelle, "KU-S3-Probe Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");

            string k = kopie.ToString(CultureInfo.InvariantCulture);
            DataRow wp = Zeile("SELECT Kuehlbetrieb, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil FROM Tab_WP WHERE ID_Projekt = " + k);
            Assert.Equal(1L, Convert.ToInt64(wp["Kuehlbetrieb"], CultureInfo.InvariantCulture));
            Assert.Equal(18L, Convert.ToInt64(wp["Kuehl_Vorlauf"], CultureInfo.InvariantCulture));
            Assert.Equal(0.07, Convert.ToDouble(wp["Kuehl_Hilfsstromanteil"], CultureInfo.InvariantCulture));
            Assert.Equal((long)STROMTRAEGER, Zahl(
                "SELECT Kuehl_ID_Carrier FROM Tab_Energieanlagen WHERE ID_Projekt = " + k + " AND ID_WP IS NOT NULL"));
        }

        // =============================================================================
        //  Teil 5 - die Laststufe der Kühlkennlinie (Kühlkonzept 5.1, Festlegung 1; 7.3)
        // =============================================================================

        /// <summary>
        /// <c>Last</c> in Modell, Leser und Schreiber: Einfügen mit und ohne Laststufe, Lesen über
        /// die Liste und den Einzelleser, Ändern auf NULL und zurück — NULL bleibt NULL, eine
        /// gepflegte Stufe bleibt stehen.
        /// </summary>
        [Fact]
        public void Die_Kuehlkennlinie_liest_und_schreibt_die_Laststufe_NULL_erhaltend()
        {
            if (!_db.Vorhanden) return;

            var mit = new KenndatenKuehlungCtrl
            {
                m_ID_WP = WP_KOPIE, m_nVorlauf = 7, m_nTemperatur = 35, m_nCOP = 2.5, m_nPkuehl = 4.5, m_nLast = 70,
            };
            Assert.True(mit.Insert());
            var ohne = new KenndatenKuehlungCtrl
            {
                m_ID_WP = WP_KOPIE, m_nVorlauf = 18, m_nTemperatur = 35, m_nCOP = 3.5, m_nPkuehl = 6.1,
            };
            Assert.Null(ohne.m_nLast);
            Assert.True(ohne.Insert());

            var liste = new KenndatenKuehlungCtrl();
            liste.ReadAll(WP_KOPIE);
            Assert.Equal(2, liste.items.Count);
            Assert.Equal(70, liste.items.Single(x => x.m_nVorlauf == 7).m_nLast);
            Assert.Null(liste.items.Single(x => x.m_nVorlauf == 18).m_nLast);

            var einzel = new KenndatenKuehlungCtrl();
            einzel.ReadSingle("SELECT * FROM Tab_Kenndaten_Kuehlung WHERE ID = " + mit.m_ID.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(70, einzel.m_nLast);
            einzel.m_nLast = null;
            einzel.m_nCOP = 2.6;
            Assert.True(einzel.Update());
            DataRow z = Zeile("SELECT COP, [Last] FROM Tab_Kenndaten_Kuehlung WHERE ID = " + mit.m_ID.ToString(CultureInfo.InvariantCulture));
            Assert.Equal(2.6, Convert.ToDouble(z["COP"], CultureInfo.InvariantCulture));
            Assert.True(z["Last"] == DBNull.Value, "Die Laststufe ist nach dem Ändern nicht NULL.");

            einzel.m_nLast = 100;
            Assert.True(einzel.Update());
            Assert.Equal(100L, Zahl("SELECT [Last] FROM Tab_Kenndaten_Kuehlung WHERE ID = " + mit.m_ID.ToString(CultureInfo.InvariantCulture)));
        }

        // -----------------------------------------------------------------------------

        private static long Zahl(string sql)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static DataRow Zeile(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            Assert.NotNull(dt);
            Assert.True(dt.Rows.Count > 0, "Keine Zeile: " + sql);
            return dt.Rows[0];
        }

        /// <summary>NULL aus der Datenbank - der Zugriff liefert dafür <c>null</c> oder <see cref="DBNull"/>.</summary>
        private static bool Leer(object wert) => wert == null || wert == DBNull.Value;

        /// <summary>Alle Zellen einer Abfrage als ein Text - für den Vorher-nachher-Vergleich.</summary>
        private static string Abdruck(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            var sb = new System.Text.StringBuilder();
            foreach (DataRow r in dt.Rows)
                sb.Append(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))))
                  .Append('\n');
            return sb.ToString();
        }

        /// <summary>Die Anlagenzeile der Wärmepumpe von 1017, gelesen über den Leser des Modells.</summary>
        private static WErzeugerModel Anlage()
        {
            var c = new WErzeugerCtrl();
            c.ReadSingle("SELECT * FROM Tab_Energieanlagen WHERE ID = " + ANLAGE.ToString(CultureInfo.InvariantCulture));
            return c;
        }
    }
}
