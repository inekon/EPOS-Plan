using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace Auslieferungsvorlage.Tests
{
    /// <summary>
    /// Die Inhaltsproben am Ergebnis EINES Laufs
    /// (<c>&lt;quelle&gt; &lt;ziel&gt; --beispiele &lt;paket&gt; --kataloge alle</c>).
    /// </summary>
    [Collection("Auslieferungsvorlage")]
    public sealed class VorlageTests : IClassFixture<Vorlage>
    {
        private readonly Vorlage _v;
        public VorlageTests(Vorlage v) { _v = v; }

        // =============================================================================
        //  P1 — Der Lauf geht durch
        // =============================================================================
        [Fact]
        public void P1_Der_Lauf_meldet_Erfolg_und_legt_Vorlage_samt_Prueflauf_ab()
        {
            if (!_v.Vorhanden) return;

            Assert.True(_v.Lauf.Code == 0, "Rueckgabecode " + _v.Lauf.Code + ":" +
                                           Environment.NewLine + _v.Lauf.Alles);
            Assert.True(File.Exists(_v.Ziel), "Die Vorlage " + _v.Ziel + " wurde nicht geschrieben.");
            Assert.True(File.Exists(_v.Ziel + ".bericht.txt"), "Der Prueflauf fehlt neben der Vorlage.");
            Assert.Contains("Schritt 2 — Projektdaten entfernen", File.ReadAllText(_v.Ziel + ".bericht.txt"));
        }

        // =============================================================================
        //  P2 — Die Quelle bleibt byte-gleich
        // =============================================================================
        [Fact]
        public void P2_Die_Quelle_bleibt_byte_gleich_und_ohne_Beidateien()
        {
            if (!_v.Vorhanden) return;

            Assert.Equal(_v.QuelleVorher, _v.QuelleNachher);
            Assert.False(File.Exists(_v.Quelle + "-wal"), "Neben der Quelle liegt eine -wal.");
            Assert.False(File.Exists(_v.Quelle + "-shm"), "Neben der Quelle liegt eine -shm.");
        }

        // =============================================================================
        //  P3 — Kein fremdes Projekt mehr, nur das Beispiel
        // =============================================================================
        /// <summary>
        /// Der DATENSCHUTZWAECHTER, und zwar mit einer EIGENEN Ableitung der
        /// Projekttabellen: Der Test fragt das Schema der erzeugten Datei selbst nach
        /// Spalten <c>ID_Projekt</c>/<c>ProjektID</c>, statt der Liste des Werkzeugs zu
        /// glauben. Ein Netz, das nur gegen sich selbst prueft, faengt nichts.
        /// </summary>
        [Fact]
        public void P3_Keine_Zeile_eines_fremden_Projekts()
        {
            if (!_v.Vorhanden) return;

            var befund = _v.Lesen(() =>
            {
                var erlaubt = new List<long>();
                foreach (DataRow r in DataRepository.GetDataTable("SELECT ID FROM Tab_Projekt").Rows)
                    erlaubt.Add(Convert.ToInt64(r["ID"]));

                string liste = erlaubt.Count == 0 ? "(-1)" : "(" + string.Join(",", erlaubt) + ")";
                var fremd = new List<string>();
                foreach (var tab in Projekttabellen())
                {
                    object n = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM \"" + tab.Item1 + "\" WHERE \"" + tab.Item2 + "\" IS NOT NULL " +
                        "AND \"" + tab.Item2 + "\" <> 0 AND \"" + tab.Item2 + "\" NOT IN " + liste);
                    if (Convert.ToInt64(n) > 0) fremd.Add(tab.Item1);
                }
                return (Erlaubt: erlaubt, Fremd: fremd);
            });

            Assert.Empty(befund.Fremd);
            Assert.Single(befund.Erlaubt);
        }

        // =============================================================================
        //  P4 — Das Beispiel ist eingespielt und lesbar
        // =============================================================================
        [Fact]
        public void P4_Das_Beispielprojekt_steht_in_der_Projektliste()
        {
            if (!_v.Vorhanden) return;

            var namen = _v.Lesen(() => ProjektCtrl.NamenListe().Select(z => z.Name).ToList());

            Assert.Single(namen);
            Assert.Equal(Vorlage.BEISPIELPROJEKT, namen[0]);
        }

        /// <summary>
        /// Ein Projektname allein waere zu wenig — er stuende auch dann da, wenn nur die
        /// Kopfzeile eingespielt worden waere. Geprueft wird deshalb, dass der
        /// Projektbaum mitgekommen ist: Energieanlagen, Einstellungen und die
        /// Klimaregion des Projekts.
        /// </summary>
        [Fact]
        public void P5_Das_Beispielprojekt_bringt_seinen_Datenbestand_mit()
        {
            if (!_v.Vorhanden) return;

            var zahlen = _v.Lesen(() =>
            {
                long id = Convert.ToInt64(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Projekt LIMIT 1"));
                return new Dictionary<string, long>
                {
                    ["Tab_Energieanlagen"] = Zaehle("Tab_Energieanlagen", "ID_Projekt", id),
                    ["Tab_Einstellungen"] = Zaehle("Tab_Einstellungen", "ID_Projekt", id),
                    ["Tab_Klimaregion"] = Zaehle("Tab_Klimaregion", "ID_Projekt", id)
                };
            });

            foreach (var kv in zahlen)
                Assert.True(kv.Value > 0, kv.Key + " ist fuer das Beispielprojekt leer.");
        }

        // =============================================================================
        //  P6 — Schemastand, Integritaet, STRICT
        // =============================================================================
        [Fact]
        public void P6_Schemastand_Integritaet_und_STRICT_stimmen()
        {
            if (!_v.Vorhanden) return;

            var befund = _v.Lesen(() => (
                Stand: Convert.ToInt32(DataRepository.ExecuteScalar("SELECT SchemaVersion FROM Tab_Applikation LIMIT 1")),
                Integritaet: Convert.ToString(DataRepository.ExecuteScalar("PRAGMA integrity_check")),
                Strict: Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' " +
                    "AND upper(trim(substr(sql, length(sql) - 6))) = 'STRICT'"))));

            Assert.Equal(SchemaStand.Zielversion, befund.Stand);
            Assert.Equal("ok", befund.Integritaet);

            // 118 seit Schemaschritt 74 (Auftrag #178, 11.09.2026): Tab_SpeicherAuslegung
            // aus Schritt 73 war die EINZIGE Fachtabelle ohne STRICT und ist neu
            // aufgebaut. Vorher 117. Die letzte Tabelle der Datei ist sqlite_sequence -
            // eine Systemtabelle, die SQLite selbst anlegt und die nie STRICT traegt.
            //
            // 119 seit Schemaschritt 75 (Auftrag #269, 14.09.2026): Tab_Nutzungsdauer
            // kommt hinzu und traegt STRICT von ihrer ersten Zeile an.
            //
            // 129 seit Schemaschritt 102 (Zapfprofilgenerator T1): die zehn Tww-Tabellen,
            // alle STRICT.
            //
            // 130 seit Schemaschritt 107 (Entscheid E30, Gebaeudesimulation):
            // Tab_ErgebnisGebaeude traegt STRICT von ihrer ersten Zeile an.
            //
            // 131 seit Schemaschritt 115 (Zapfprofilgenerator T2): Tab_TwwZapfkategorie_STAMM,
            // STRICT von ihrer ersten Zeile an.
            //
            // 132 seit Schemaschritt 125 (Zapfprofilgenerator T3 "Typtage", Stufe Z4b):
            // Tab_TwwTyptag_IMPORT, STRICT von ihrer ersten Zeile an. Die Tabelle ist in der
            // Vorlage LEER - sie nimmt die Typtage des lizenzierten Anwenders auf, nie eine
            // Auslieferungszeile.
            Assert.Equal(132, befund.Strict);
        }

        // =============================================================================
        //  P6b — Die Nutzungsdauern bleiben in der Auslieferung
        // =============================================================================
        /// <summary>
        /// <c>Tab_Nutzungsdauer</c> (Schemaschritt 75) haengt an keinem Projekt und ist
        /// kein <c>*_STAMM</c>-Katalog — sie faellt also durch beide Regeln des
        /// Vorlagenbaus und bleibt VOLLSTAENDIG stehen. Genau das soll sie: Die 28
        /// Auslieferungszeilen sind der Grund, warum eine neue Position ueberhaupt eine
        /// Nutzungsdauer bekommt.
        ///
        /// <para>Der Fall ist die Gegenprobe dazu. Faellt er rot aus, hat die
        /// Projektbereinigung die Tabelle mitgenommen — dann fehlt der Auslieferung ihre
        /// AfA-Tabelle, und jede neue Kostenposition entstuende wieder ohne
        /// Nutzungsdauer.</para>
        /// </summary>
        [Fact]
        public void P6b_Die_Nutzungsdauern_stehen_vollstaendig_in_der_Vorlage()
        {
            if (!_v.Vorhanden) return;

            var befund = _v.Lesen(() => (
                Zeilen: Convert.ToInt64(DataRepository.ExecuteScalar(
                    NutzungsdauerSchema.Zaehlung())),
                Auslieferung: Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"Tab_Nutzungsdauer\" WHERE \"ReadOnly\" = 1")),
                Standard: Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM \"Tab_Nutzungsdauer\" WHERE \"IstStandard\" = 1")),
                Verweise: Convert.ToInt64(DataRepository.ExecuteScalar(
                    NutzungsdauerSchema.ZaehlungZuordnung()))));

            Assert.Equal(NutzungsdauerSchema.Saat.Length, befund.Zeilen);
            Assert.Equal(NutzungsdauerSchema.Saat.Length, befund.Auslieferung);

            // Genau eine Standardzeile je Technik - zehn Kostenkomponenten.
            Assert.Equal(10, befund.Standard);

            // Die Vorlagenpositionen behalten ihre Positionsart.
            Assert.True(befund.Verweise > 0,
                        "Keine Auslieferungsposition traegt mehr eine Positionsart.");
        }

        // =============================================================================
        //  P7 — Personenbezug abgeraeumt
        // =============================================================================
        /// <summary>
        /// <c>Tab_Applikation</c> haengt an keinem Projekt und traegt in der
        /// Entwicklungsdatenbank den Namen des zuletzt geoeffneten KUNDENprojekts. Die
        /// Probe stellt sicher, dass die Bereinigung ihn nicht uebersieht — und dass der
        /// Schemastand daneben stehen bleibt.
        /// </summary>
        [Fact]
        public void P7_Tab_Applikation_nennt_kein_Projekt_mehr()
        {
            if (!_v.Vorhanden) return;

            var zeile = _v.Lesen(() => DataRepository.GetDataTable(
                "SELECT Projektname, ID_Projekt, Beschreibung, Icon, SchemaVersion FROM Tab_Applikation").Rows[0]);

            Assert.Equal("", Convert.ToString(zeile["Projektname"]));
            Assert.Equal(0, Convert.ToInt32(zeile["ID_Projekt"]));
            Assert.Equal("", Convert.ToString(zeile["Beschreibung"]));
            Assert.Equal("", Convert.ToString(zeile["Icon"]));
            Assert.Equal(SchemaStand.Zielversion, Convert.ToInt32(zeile["SchemaVersion"]));
        }

        // =============================================================================
        //  P8 — Eine Datei, kein Anhang
        // =============================================================================
        [Fact]
        public void P8_Neben_der_Vorlage_liegt_keine_Beidatei()
        {
            if (!_v.Vorhanden) return;

            Assert.False(File.Exists(_v.Ziel + "-wal"));
            Assert.False(File.Exists(_v.Ziel + "-shm"));
        }

        // -----------------------------------------------------------------------------
        private static long Zaehle(string tabelle, string spalte, long id) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" = " + id));

        /// <summary>
        /// Tabellen mit eigener Projektspalte, ohne Kataloge und ohne die Statustabelle —
        /// aus dem Schema der GEOEFFNETEN Datei, nicht aus einer Liste.
        /// </summary>
        internal static List<Tuple<string, string>> Projekttabellen()
        {
            var ergebnis = new List<Tuple<string, string>>();
            DataTable tabellen = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name");
            foreach (DataRow r in tabellen.Rows)
            {
                string t = Convert.ToString(r["name"]);
                if (t.EndsWith("_STAMM", StringComparison.Ordinal) || t == "Tab_Applikation" || t == "Tab_Projekt")
                    continue;
                foreach (string s in new[] { "ID_Projekt", "ProjektID" })
                    if (DataRepository.SpalteVorhanden(t, s)) { ergebnis.Add(Tuple.Create(t, s)); break; }
            }
            return ergebnis;
        }
    }
}
