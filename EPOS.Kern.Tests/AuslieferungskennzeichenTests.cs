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
    /// <b>„Schloss setzen…" / „Schloss aufheben…"</b> — das Auslieferungskennzeichen eines
    /// Katalogsatzes, vom Anwender umschaltbar (Konzept Administrationsdialoge, Entscheid
    /// <b>AD-Q15</b>, löst AD-Q11 ab).
    ///
    /// <para><b>Geprüft gegen eine Arbeitskopie der Testdatenbank</b> (je Fall eine eigene;
    /// die Datei unter <c>Referenzlaeufe/</c> bleibt unberührt): je Katalog der Verwaltungen
    /// Aufheben und Setzen über den Einzeiler des Stamm-Controllers, den die Hülle ruft; kein
    /// Wert des Satzes ändert sich, keine Kindzeile, kein Nachbarsatz. Dazu die benannten
    /// Ablehnungen (Tww, Tabelle ohne Kennzeichen, unbekannter Katalog, Lesemodus der Lizenz,
    /// fehlender Satz) und die eine Transaktion: Fehlt ein Satz, bleibt auch der davor, wie er
    /// war.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AuslieferungskennzeichenTests
    {
        /// <summary>
        /// Der Weg je Kopftabelle — genau der Einzeiler, den die Hülle der Verwaltung ruft.
        /// </summary>
        private static Auslieferungskennzeichen.Ergebnis Schalten(string tabelle, IReadOnlyList<int> ids, bool gesperrt)
            => tabelle switch
            {
                HeizkesselStammCtrl.TABLE => HeizkesselStammCtrl.SchlossSetzen(ids, gesperrt),
                BHKWStammCtrl.TABLE => BHKWStammCtrl.SchlossSetzen(ids, gesperrt),
                SolarkollektorenStammCtrl.TABLE => SolarkollektorenStammCtrl.SchlossSetzen(ids, gesperrt),
                PufferSpStammCtrl.TABLE => PufferSpStammCtrl.SchlossSetzen(ids, gesperrt),
                PhotovoltaikStammCtrl.TABLE => PhotovoltaikStammCtrl.SchlossSetzen(ids, gesperrt),
                WechselrichterStammCtrl.TABLE => WechselrichterStammCtrl.SchlossSetzen(ids, gesperrt),
                StromspeicherStammCtrl.TABLE => StromspeicherStammCtrl.SchlossSetzen(ids, gesperrt),
                WPStammCtrl.TABLE => WPStammCtrl.SchlossSetzen(ids, gesperrt),
                GebaeudeStammCtrl.TABLE => GebaeudeStammCtrl.SchlossSetzen(ids, gesperrt),
                KlimaregionStammCtrl.TAB_REGION_STAMM => KlimaregionStammCtrl.SchlossSetzen(ids, gesperrt),
                BrauchwasserStammCtrl.TABLE => BedarfStammCtrl.SchlossSetzen(BedarfsArt.Brauchwasser, ids, gesperrt),
                ProzesswaermeStammCtrl.TABLE => BedarfStammCtrl.SchlossSetzen(BedarfsArt.Prozesswaerme, ids, gesperrt),
                StromverbraucherStammCtrl.TABLE => BedarfStammCtrl.SchlossSetzen(BedarfsArt.Stromverbraucher, ids, gesperrt),
                StromganglinieStammCtrl.HEAD_STAMM => ZeitreihenKatalogCtrl.SchlossSetzen(Zeitreihenart.Stromganglinie, ids, gesperrt),
                SolarganglinieStammCtrl.HEAD_STAMM => ZeitreihenKatalogCtrl.SchlossSetzen(Zeitreihenart.Solarganglinie, ids, gesperrt),
                WaermebedarfStammCtrl.HEAD_STAMM => ZeitreihenKatalogCtrl.SchlossSetzen(Zeitreihenart.Waermebedarf, ids, gesperrt),
                TagVCtrl.TABLE => TagVCtrl.SchlossSetzen(ids, gesperrt),
                _ => throw new ArgumentOutOfRangeException(nameof(tabelle))
            };

        private static KatalogDefinition Definition(string tabelle)
        {
            KatalogDefinition def = KatalogRegistry.FindeTabelle(tabelle);
            Assert.True(def != null, tabelle + " steht nicht in der KatalogRegistry.");
            return def;
        }

        /// <summary>Der erste Satz der Tabelle (kleinste ID).</summary>
        private static DataRow Erster(KatalogDefinition def)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + def.Tabelle + "] ORDER BY [" + def.IdSpalte + "] LIMIT 1");
            Assert.True(dt.Rows.Count == 1, def.Tabelle + " ist leer.");
            return dt.Rows[0];
        }

        private static DataRow Zeile(KatalogDefinition def, int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + def.Tabelle + "] WHERE [" + def.IdSpalte + "] = ?", new DbParam("@id", id));
            Assert.True(dt.Rows.Count == 1, def.Tabelle + ": keine Zeile " + id);
            return dt.Rows[0];
        }

        private static bool Schloss(KatalogDefinition def, DataRow r)
        {
            bool ro = Convert.ToInt32(r["ReadOnly"], CultureInfo.InvariantCulture) != 0;
            if (def.SchlossGegenspalte.Length == 0) return ro;
            return ro || Convert.ToInt32(r[def.SchlossGegenspalte], CultureInfo.InvariantCulture) == 0;
        }

        /// <summary>Alle Spalten außer dem Kennzeichen (und der Gegenspalte) als Vergleichstext.</summary>
        private static string Werte(KatalogDefinition def, DataRow r)
        {
            var teile = new List<string>();
            foreach (DataColumn c in r.Table.Columns)
            {
                if (string.Equals(c.ColumnName, "ReadOnly", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(c.ColumnName, def.SchlossGegenspalte, StringComparison.OrdinalIgnoreCase)) continue;
                teile.Add(c.ColumnName + "=" + Convert.ToString(r[c], CultureInfo.InvariantCulture));
            }
            return string.Join("|", teile);
        }

        /// <summary>Die ganze Tabelle als Text — Nachbarsätze und Kindzeilen dürfen sich nicht rühren.</summary>
        private static string Tabelle(string tabelle, string sortierung)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM [" + tabelle + "] ORDER BY " + sortierung);
            var zeilen = new List<string>();
            foreach (DataRow r in dt.Rows)
                zeilen.Add(string.Join("|", r.ItemArray.Select(o => Convert.ToString(o, CultureInfo.InvariantCulture))));
            return string.Join("\n", zeilen);
        }

        // =================================================================================
        //  1 — Je Katalog der Verwaltungen: aufheben und setzen, kein Wert ändert sich
        // =================================================================================

        [Theory]
        [InlineData(HeizkesselStammCtrl.TABLE)]
        [InlineData(BHKWStammCtrl.TABLE)]
        [InlineData(SolarkollektorenStammCtrl.TABLE)]
        [InlineData(PufferSpStammCtrl.TABLE)]
        [InlineData(PhotovoltaikStammCtrl.TABLE)]
        [InlineData(WechselrichterStammCtrl.TABLE)]
        [InlineData(StromspeicherStammCtrl.TABLE)]
        [InlineData(WPStammCtrl.TABLE)]
        [InlineData(GebaeudeStammCtrl.TABLE)]
        [InlineData(KlimaregionStammCtrl.TAB_REGION_STAMM)]
        [InlineData(BrauchwasserStammCtrl.TABLE)]
        [InlineData(ProzesswaermeStammCtrl.TABLE)]
        [InlineData(StromverbraucherStammCtrl.TABLE)]
        [InlineData(StromganglinieStammCtrl.HEAD_STAMM)]
        [InlineData(SolarganglinieStammCtrl.HEAD_STAMM)]
        [InlineData(WaermebedarfStammCtrl.HEAD_STAMM)]
        [InlineData(TagVCtrl.TABLE)]
        public void Das_Schloss_laesst_sich_aufheben_und_wieder_setzen_ohne_dass_sich_ein_Wert_aendert(string tabelle)
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                KatalogDefinition def = Definition(tabelle);
                DataRow satz = Erster(def);
                int id = Convert.ToInt32(satz[def.IdSpalte], CultureInfo.InvariantCulture);
                bool vorher = Schloss(def, satz);
                string werte = Werte(def, satz);
                int anzahl = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]"));

                // Hin: in den anderen Zustand ...
                Auslieferungskennzeichen.Ergebnis hin = Schalten(tabelle, new[] { id }, !vorher);
                Assert.True(hin.Ok, hin.Meldung);
                Assert.Equal(new[] { id }, hin.Geaendert);
                Assert.Empty(hin.Unveraendert);
                DataRow mitte = Zeile(def, id);
                Assert.Equal(!vorher, Schloss(def, mitte));
                Assert.Equal(werte, Werte(def, mitte));

                // ... und zurück: wieder der Ausgangszustand, die Werte wie zuvor.
                Auslieferungskennzeichen.Ergebnis zurueck = Schalten(tabelle, new[] { id }, vorher);
                Assert.True(zurueck.Ok, zurueck.Meldung);
                Assert.Equal(new[] { id }, zurueck.Geaendert);
                DataRow nachher = Zeile(def, id);
                Assert.Equal(vorher, Schloss(def, nachher));
                Assert.Equal(werte, Werte(def, nachher));
                Assert.Equal(anzahl, Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]")));
            }
        }

        /// <summary>
        /// <b>Jeder Katalog der Registry außer den Tww-Katalogen</b> lässt sich über den
        /// Schlüssel schalten — auch die drei Typprofile, die keine eigene Verwaltung haben.
        /// </summary>
        [Fact]
        public void Jeder_Registrykatalog_ohne_Freigabestatus_laesst_sich_ueber_den_Schluessel_schalten()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                foreach (KatalogDefinition def in KatalogRegistry.Alle.Where(k => !k.SchlossAusStatus))
                {
                    DataRow satz = Erster(def);
                    int id = Convert.ToInt32(satz[def.IdSpalte], CultureInfo.InvariantCulture);
                    bool vorher = Schloss(def, satz);

                    Auslieferungskennzeichen.Ergebnis e = Auslieferungskennzeichen.Setzen(def.Schluessel, new[] { id }, !vorher);
                    Assert.True(e.Ok, def.Schluessel + ": " + e.Meldung);
                    Assert.Equal(!vorher, Schloss(def, Zeile(def, id)));
                }
            }
        }

        [Fact]
        public void Ein_Satz_im_Zielzustand_bleibt_unveraendert_und_wird_so_gemeldet()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                // Die BHKW der Testdatenbank sind alle ausgeliefert.
                KatalogDefinition def = Definition(BHKWStammCtrl.TABLE);
                int id = Convert.ToInt32(Erster(def)["ID"], CultureInfo.InvariantCulture);
                Assert.True(Schloss(def, Zeile(def, id)));

                Auslieferungskennzeichen.Ergebnis e = BHKWStammCtrl.SchlossSetzen(new[] { id, id }, true);

                Assert.True(e.Ok, e.Meldung);
                Assert.Empty(e.Geaendert);
                Assert.Equal(new[] { id }, e.Unveraendert);   // doppelt genannt, einmal gezählt
            }
        }

        [Fact]
        public void Mehrere_Saetze_in_einem_Aufruf_nur_die_gewaehlten_und_keine_Nachbarn()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                KatalogDefinition def = Definition(BHKWStammCtrl.TABLE);
                DataTable dt = DataRepository.GetDataTable("SELECT ID FROM [" + def.Tabelle + "] ORDER BY ID LIMIT 3");
                int[] ids = dt.Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r["ID"])).ToArray();
                Assert.Equal(3, ids.Length);

                Auslieferungskennzeichen.Ergebnis e = BHKWStammCtrl.SchlossSetzen(ids.Take(2).ToList(), false);

                Assert.True(e.Ok, e.Meldung);
                Assert.Equal(ids.Take(2), e.Geaendert);
                Assert.False(Schloss(def, Zeile(def, ids[0])));
                Assert.False(Schloss(def, Zeile(def, ids[1])));
                Assert.True(Schloss(def, Zeile(def, ids[2])));   // der Nachbar bleibt gesperrt
            }
        }

        [Fact]
        public void Eine_leere_Wahl_schreibt_nichts_und_ist_kein_Fehler()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                Auslieferungskennzeichen.Ergebnis e = BHKWStammCtrl.SchlossSetzen(Array.Empty<int>(), false);

                Assert.True(e.Ok);
                Assert.Empty(e.Geaendert);
                Assert.Empty(e.Unveraendert);
            }
        }

        // =================================================================================
        //  2 — Nach dem Aufheben ist der Satz ein eigener: Liste, Speichern, Löschen
        // =================================================================================

        /// <summary>
        /// <b>Die Verwaltung liest den neuen Zustand</b> aus derselben Zeile wie das Schloss
        /// (<c>Katalogfilterzeile.Geschuetzt</c>), und der Stamm-Controller lässt
        /// Speichern und Löschen wieder zu (<c>IsReadOnly</c>).
        /// </summary>
        [Fact]
        public void Nach_dem_Aufheben_zeigt_die_Liste_keinen_Schutz_und_der_Controller_sperrt_nicht()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                KatalogDefinition def = Definition(BHKWStammCtrl.TABLE);
                DataRow satz = Erster(def);
                int id = Convert.ToInt32(satz["ID"], CultureInfo.InvariantCulture);
                string name = Convert.ToString(satz["Bezeichner"], CultureInfo.InvariantCulture);
                var ctrl = new BHKWStammCtrl();
                Assert.True(ctrl.IsReadOnly(id));

                Assert.True(BHKWStammCtrl.SchlossSetzen(new[] { id }, false).Ok);

                Assert.False(ctrl.IsReadOnly(id));
                Assert.False(ctrl.IsReadOnly(name));
                Assert.False(ctrl.Katalogfilterzeilen().Single(z => z.Id == id).Geschuetzt);

                Assert.True(BHKWStammCtrl.SchlossSetzen(new[] { id }, true).Ok);
                Assert.True(ctrl.IsReadOnly(id));
                Assert.True(ctrl.Katalogfilterzeilen().Single(z => z.Id == id).Geschuetzt);
            }
        }

        /// <summary>
        /// <b>Nur der Kopfsatz</b>: Die Kennlinien der Wärmepumpe führen <c>ReadOnly</c>
        /// ebenfalls, werten es aber nicht aus — sie bleiben, wie sie sind.
        /// </summary>
        [Fact]
        public void Die_Kindzeilen_bleiben_unberuehrt()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                object ausgeliefert = DataRepository.ExecuteScalar(
                    "SELECT ID FROM [" + WPStammCtrl.TABLE + "] WHERE ReadOnly = 1 ORDER BY ID LIMIT 1");
                Assert.NotNull(ausgeliefert);
                int id = Convert.ToInt32(ausgeliefert, CultureInfo.InvariantCulture);
                string kennlinien = Tabelle(WPStammCtrl.CURVE, "ID");
                string kuehlung = Tabelle(WPStammCtrl.CURVE_K, "ID");

                Assert.True(WPStammCtrl.SchlossSetzen(new[] { id }, false).Ok);

                Assert.Equal(kennlinien, Tabelle(WPStammCtrl.CURVE, "ID"));
                Assert.Equal(kuehlung, Tabelle(WPStammCtrl.CURVE_K, "ID"));
            }
        }

        // =================================================================================
        //  3 — Der Gebäudetyp: ReadOnly UND Veraenderbar
        // =================================================================================

        /// <summary>
        /// <b>Das Schloss des Gebäudetyps</b> ist <c>ReadOnly</c> ODER nicht
        /// <c>Veraenderbar</c> — die Testdatenbank führt acht Typen mit
        /// <c>Veraenderbar = 0</c> und <c>ReadOnly = 0</c>. Aufheben setzt beide Spalten auf
        /// „änderbar", Setzen beide auf „gesperrt"; die 192 Stundenwerte rühren sich nicht.
        /// </summary>
        [Fact]
        public void Der_Gebaeudetyp_schaltet_ReadOnly_und_Veraenderbar()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                object gesperrt = DataRepository.ExecuteScalar(
                    "SELECT ID FROM [" + TagVCtrl.TABLE + "] WHERE Veraenderbar = 0 AND ReadOnly = 0 ORDER BY ID LIMIT 1");
                Assert.NotNull(gesperrt);
                int id = Convert.ToInt32(gesperrt, CultureInfo.InvariantCulture);
                Assert.True(TagVCtrl.Katalogfilterzeilen().Single(z => z.Id == id).Geschuetzt);
                string stunden = Tabelle(TagVCtrl.TABLE_DATEN, "ID");

                Auslieferungskennzeichen.Ergebnis auf = TagVCtrl.SchlossSetzen(new[] { id }, false);
                Assert.True(auf.Ok, auf.Meldung);
                Assert.Equal(new[] { id }, auf.Geaendert);
                DataRow r = Zeile(Definition(TagVCtrl.TABLE), id);
                Assert.Equal(0, Convert.ToInt32(r["ReadOnly"]));
                Assert.Equal(1, Convert.ToInt32(r["Veraenderbar"]));
                Assert.False(TagVCtrl.Katalogfilterzeilen().Single(z => z.Id == id).Geschuetzt);

                Auslieferungskennzeichen.Ergebnis zu = TagVCtrl.SchlossSetzen(new[] { id }, true);
                Assert.True(zu.Ok, zu.Meldung);
                r = Zeile(Definition(TagVCtrl.TABLE), id);
                Assert.Equal(1, Convert.ToInt32(r["ReadOnly"]));
                Assert.Equal(0, Convert.ToInt32(r["Veraenderbar"]));
                Assert.True(TagVCtrl.Katalogfilterzeilen().Single(z => z.Id == id).Geschuetzt);

                Assert.Equal(stunden, Tabelle(TagVCtrl.TABLE_DATEN, "ID"));
            }
        }

        // =================================================================================
        //  4 — Benannte Ablehnungen: nichts ist geschrieben
        // =================================================================================

        /// <summary>
        /// <b>Die Tww-Kataloge</b>: Ihr <c>ReadOnly</c> folgt dem Freigabestatus — das
        /// Umschalten lehnt benannt ab, der Satz bleibt, wie er war.
        /// </summary>
        [Theory]
        [InlineData("TWW_NUTZUNGSART")]
        [InlineData("TWW_TAGESGANGSATZ")]
        [InlineData("TWW_BEDARFSTAG")]
        public void Die_Tww_Kataloge_werden_benannt_abgelehnt(string schluessel)
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                KatalogDefinition def = KatalogRegistry.Finde(schluessel);
                Assert.True(def.SchlossAusStatus);
                DataRow satz = Erster(def);
                int id = Convert.ToInt32(satz[def.IdSpalte], CultureInfo.InvariantCulture);
                bool vorher = Schloss(def, satz);

                Auslieferungskennzeichen.Ergebnis e = Auslieferungskennzeichen.Setzen(schluessel, new[] { id }, !vorher);

                Assert.False(e.Ok);
                Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ADM_SCHLOSS_STATUS, e.Meldung);
                Assert.Empty(e.Geaendert);
                Assert.Equal(vorher, Schloss(def, Zeile(def, id)));
            }
        }

        /// <summary>
        /// <b>Eine Tabelle ohne Kennzeichen</b> (die Klimadaten einer Region führen kein
        /// <c>ReadOnly</c>) wird benannt abgelehnt, nicht mit einer SQL-Ausnahme.
        /// </summary>
        [Fact]
        public void Eine_Tabelle_ohne_Kennzeichen_wird_benannt_abgelehnt()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                var def = new KatalogDefinition
                {
                    Schluessel = "KLIMAREGION",
                    Tabelle = KlimaregionStammCtrl.TAB_KLIMADATEN_STAMM,
                    IdSpalte = "ID_Klimadaten"
                };
                string vorher = Tabelle(def.Tabelle, "ID_Klimadaten LIMIT 5");

                Auslieferungskennzeichen.Ergebnis e = Auslieferungskennzeichen.Setzen(def, new[] { 1 }, true);

                Assert.False(e.Ok);
                Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.ADM_SCHLOSS_OHNE_KENNZEICHEN,
                                           KatalogRegistry.Anzeige("KLIMAREGION")), e.Meldung);
                Assert.Equal(vorher, Tabelle(def.Tabelle, "ID_Klimadaten LIMIT 5"));
            }
        }

        [Fact]
        public void Ein_unbekannter_Katalog_wird_benannt_abgelehnt()
        {
            Auslieferungskennzeichen.Ergebnis e = Auslieferungskennzeichen.Setzen("GIBT_ES_NICHT", new[] { 1 }, false);
            Assert.False(e.Ok);
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.ADM_SCHLOSS_KATALOG_UNBEKANNT,
                                       "GIBT_ES_NICHT"), e.Meldung);

            Auslieferungskennzeichen.Ergebnis t = Auslieferungskennzeichen.SetzenInTabelle("Tab_Klimadaten_STAMM", new[] { 1 }, false);
            Assert.False(t.Ok);
        }

        /// <summary>
        /// <b>Der Lesemodus der Lizenz</b> sperrt das Umschalten benannt — wie jedes Schreiben
        /// über die Schreibnaht; der Satz bleibt, wie er war.
        /// </summary>
        [Fact]
        public void Im_Lesemodus_der_Lizenz_wird_benannt_abgelehnt()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                KatalogDefinition def = Definition(BHKWStammCtrl.TABLE);
                int id = Convert.ToInt32(Erster(def)["ID"], CultureInfo.InvariantCulture);

                Func<bool> vorher = Schreibnaht.Schreibrecht;
                Schreibnaht.Schreibrecht = () => false;
                try
                {
                    Auslieferungskennzeichen.Ergebnis e = BHKWStammCtrl.SchlossSetzen(new[] { id }, false);

                    Assert.False(e.Ok);
                    Assert.Equal(WindowsFormsApplication1.MyResource.Resource.LIZ_LESEMODUS_SPERRE, e.Meldung);
                }
                finally
                {
                    Schreibnaht.Schreibrecht = vorher;
                }

                Assert.True(Schloss(def, Zeile(def, id)));
            }
        }

        [Fact]
        public void Eine_unbekannte_ID_wird_benannt_abgelehnt()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                Auslieferungskennzeichen.Ergebnis e = BHKWStammCtrl.SchlossSetzen(new[] { 987654 }, false);

                Assert.False(e.Ok);
                Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.ADM_SCHLOSS_SATZ_FEHLT, 987654),
                             e.Meldung);
                Assert.Empty(e.Geaendert);
            }
        }

        /// <summary>
        /// <b>EINE Transaktion</b>: Der erste Satz ist schon geschaltet, als der zweite fehlt —
        /// der Vorgang rollt zurück, auch der erste bleibt gesperrt.
        /// </summary>
        [Fact]
        public void Fehlt_ein_Satz_bleibt_auch_der_davor_wie_er_war()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                KatalogDefinition def = Definition(BHKWStammCtrl.TABLE);
                int id = Convert.ToInt32(Erster(def)["ID"], CultureInfo.InvariantCulture);
                string vorher = Tabelle(def.Tabelle, "ID");

                Auslieferungskennzeichen.Ergebnis e = BHKWStammCtrl.SchlossSetzen(new[] { id, 987654 }, false);

                Assert.False(e.Ok);
                Assert.True(Schloss(def, Zeile(def, id)));
                Assert.Equal(vorher, Tabelle(def.Tabelle, "ID"));
            }
        }
    }
}
