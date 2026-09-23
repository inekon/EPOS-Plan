using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Duplizieren…" in den acht Gerätekatalogen</b> (Konzept
    /// Administrationsdialoge, Entscheid <b>AD-Q11</b> vom 23.09.2026: Auslieferungssätze
    /// werden nie überschrieben — wer einen ändern will, dupliziert ihn).
    ///
    /// <para><b>Geprüft je Stamm-Controller gegen die Testdatenbank:</b> Die Kopie trägt den
    /// neuen Namen, ist KEIN Auslieferungssatz (<c>ReadOnly = 0</c>) und stimmt in jeder
    /// übrigen Spalte mit dem Original überein; das Original bleibt, wie es war. Bei der
    /// Wärmepumpe kommen die Kennlinien (Heizen und Kühlen) Zeile für Zeile mit.</para>
    ///
    /// <para><b>Jeder schreibende Fall hat seine EIGENE Arbeitskopie</b> — die Regel seit
    /// W11a; die Datei unter <c>Referenzlaeufe/</c> bleibt unberührt.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogduplizierenTests
    {
        /// <summary>Die Spalten, die eine Kopie NICHT übernimmt.</summary>
        private static readonly string[] KOPF = { "ID", "Bezeichner", "ReadOnly" };

        /// <summary>Der Weg je Tabelle — genau der Controller, den die Hülle ruft.</summary>
        private static Katalogkopie.Ergebnis Duplizieren(string tabelle, int id, string name) => tabelle switch
        {
            HeizkesselStammCtrl.TABLE => HeizkesselStammCtrl.Duplizieren(id, name),
            BHKWStammCtrl.TABLE => BHKWStammCtrl.Duplizieren(id, name),
            SolarkollektorenStammCtrl.TABLE => SolarkollektorenStammCtrl.Duplizieren(id, name),
            PufferSpStammCtrl.TABLE => PufferSpStammCtrl.Duplizieren(id, name),
            PhotovoltaikStammCtrl.TABLE => PhotovoltaikStammCtrl.Duplizieren(id, name),
            WechselrichterStammCtrl.TABLE => WechselrichterStammCtrl.Duplizieren(id, name),
            StromspeicherStammCtrl.TABLE => StromspeicherStammCtrl.Duplizieren(id, name),
            WPStammCtrl.TABLE => WPStammCtrl.Duplizieren(id, name),
            // Stufe 3: die drei Bedarfskataloge (zurueckgestellt aus Stufe 2).
            BrauchwasserStammCtrl.TABLE => BedarfStammCtrl.Duplizieren(BedarfsArt.Brauchwasser, id, name),
            ProzesswaermeStammCtrl.TABLE => BedarfStammCtrl.Duplizieren(BedarfsArt.Prozesswaerme, id, name),
            StromverbraucherStammCtrl.TABLE => BedarfStammCtrl.Duplizieren(BedarfsArt.Stromverbraucher, id, name),
            // Stufe 5: die Gebaeudeverwaltung (V16).
            GebaeudeStammCtrl.TABLE => GebaeudeStammCtrl.Duplizieren(id, name),
            _ => throw new ArgumentOutOfRangeException(nameof(tabelle))
        };

        /// <summary>
        /// Die erste Zeile der Tabelle, ein Auslieferungssatz zuerst — das ist der Fall, für
        /// den es „Duplizieren…" gibt (BHKW, Wechselrichter, Wärmepumpe führen welche).
        /// </summary>
        private static DataRow Vorlage(string tabelle)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + tabelle + "] ORDER BY ReadOnly DESC, ID LIMIT 1");
            Assert.True(dt.Rows.Count == 1, tabelle + " ist leer.");
            return dt.Rows[0];
        }

        private static DataRow Zeile(string tabelle, int id)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + tabelle + "] WHERE ID = ?", new DbParam("@id", id));
            Assert.True(dt.Rows.Count == 1, tabelle + ": keine Zeile " + id);
            return dt.Rows[0];
        }

        private static int Anzahl(string tabelle)
            => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]"));

        private static bool Schreibgeschuetzt(DataRow r) => Convert.ToInt32(r["ReadOnly"]) != 0;

        // =================================================================================
        //  1 — Je Katalog: die Kopie ist ein eigener Satz, sonst gleich
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
        [InlineData(BrauchwasserStammCtrl.TABLE)]
        [InlineData(ProzesswaermeStammCtrl.TABLE)]
        [InlineData(StromverbraucherStammCtrl.TABLE)]
        [InlineData(GebaeudeStammCtrl.TABLE)]
        public void Duplizieren_legt_einen_eigenen_Satz_mit_allen_Spalten_an(string tabelle)
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                DataRow original = Vorlage(tabelle);
                int alteId = Convert.ToInt32(original["ID"]);
                bool warGeschuetzt = Schreibgeschuetzt(original);
                int vorher = Anzahl(tabelle);

                Katalogkopie.Ergebnis e = Duplizieren(tabelle, alteId, "AD-Q11 Kopie");

                Assert.True(e.Ok, e.Meldung);
                Assert.Equal("AD-Q11 Kopie", e.Name);
                Assert.True(e.Id > 0 && e.Id != alteId);
                Assert.Equal(vorher + 1, Anzahl(tabelle));

                DataRow kopie = Zeile(tabelle, e.Id);
                Assert.Equal("AD-Q11 Kopie", Convert.ToString(kopie["Bezeichner"]));
                Assert.False(Schreibgeschuetzt(kopie));

                foreach (DataColumn spalte in original.Table.Columns)
                {
                    if (KOPF.Contains(spalte.ColumnName, StringComparer.OrdinalIgnoreCase)) continue;
                    Assert.True(Equals(original[spalte.ColumnName], kopie[spalte.ColumnName]),
                                tabelle + "." + spalte.ColumnName + ": " + original[spalte.ColumnName] +
                                " gegen " + kopie[spalte.ColumnName]);
                }

                // Das Original bleibt, wie es war - auch sein Schreibschutz.
                Assert.Equal(warGeschuetzt, Schreibgeschuetzt(Zeile(tabelle, alteId)));
            }
        }

        // =================================================================================
        //  2 — Die Wärmepumpe: Kennlinien kommen mit
        // =================================================================================

        /// <summary>
        /// <b>Die Kennlinien Heizen UND Kühlen kommen Zeile für Zeile mit</b> — die Wärmepumpe
        /// 42 der Testdatenbank führt 40 Heiz- und 60 Kühlpunkte, die 25 ist ein
        /// Auslieferungssatz mit 40 Heizpunkten. Die Kennlinienzeilen der Kopie sind ebenso
        /// kein Auslieferungsbestand.
        /// </summary>
        [Theory]
        [InlineData(42)]
        [InlineData(25)]
        public void Die_Waermepumpe_nimmt_ihre_Kennlinien_mit(int alteId)
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                const string heiz = "SELECT Vorlauf, Temperatur, COP, Ptherm FROM Tab_Kenndaten_STAMM WHERE ID_WP = ? ORDER BY ID";
                const string kuehl = "SELECT Vorlauf, Temperatur, COP, Pkuehl, [Last] FROM Tab_Kenndaten_Kuehlung_STAMM WHERE ID_WP = ? ORDER BY ID";

                DataTable heizAlt = DataRepository.GetDataTable(heiz, new DbParam("@id", alteId));
                DataTable kuehlAlt = DataRepository.GetDataTable(kuehl, new DbParam("@id", alteId));
                Assert.True(heizAlt.Rows.Count > 0, "Die Probe braucht eine Wärmepumpe mit Kennlinie.");

                Katalogkopie.Ergebnis e = WPStammCtrl.Duplizieren(alteId, "AD-Q11 WP " + alteId);
                Assert.True(e.Ok, e.Meldung);

                DataTable heizNeu = DataRepository.GetDataTable(heiz, new DbParam("@id", e.Id));
                DataTable kuehlNeu = DataRepository.GetDataTable(kuehl, new DbParam("@id", e.Id));

                Assert.Equal(heizAlt.Rows.Count, heizNeu.Rows.Count);
                Assert.Equal(kuehlAlt.Rows.Count, kuehlNeu.Rows.Count);
                GleicheZeilen(heizAlt, heizNeu);
                GleicheZeilen(kuehlAlt, kuehlNeu);

                object geschuetzt = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Kenndaten_STAMM WHERE ID_WP = ? AND ReadOnly <> 0",
                    new DbParam("@id", e.Id));
                Assert.Equal(0, Convert.ToInt32(geschuetzt));

                // Das Original behält seine Kennlinie.
                Assert.Equal(heizAlt.Rows.Count,
                             DataRepository.GetDataTable(heiz, new DbParam("@id", alteId)).Rows.Count);
            }
        }

        private static void GleicheZeilen(DataTable a, DataTable b)
        {
            for (int i = 0; i < a.Rows.Count; i++)
                for (int s = 0; s < a.Columns.Count; s++)
                    Assert.True(Equals(a.Rows[i][s], b.Rows[i][s]),
                                "Zeile " + i + ", Spalte " + a.Columns[s].ColumnName);
        }

        // =================================================================================
        //  3 — Was abgelehnt wird
        // =================================================================================

        /// <summary>
        /// Ein vergebener Name wird abgelehnt — der Bezeichner hat keinen eindeutigen Index,
        /// die Prüfung ist die einzige Wache gegen eine neue Dublette. Geschrieben wird nichts.
        /// </summary>
        [Fact]
        public void Ein_vergebener_Name_wird_abgelehnt_und_nichts_geschrieben()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                DataRow original = Vorlage(BHKWStammCtrl.TABLE);
                int vorher = Anzahl(BHKWStammCtrl.TABLE);

                Katalogkopie.Ergebnis e = BHKWStammCtrl.Duplizieren(
                    Convert.ToInt32(original["ID"]), Convert.ToString(original["Bezeichner"]));

                Assert.False(e.Ok);
                Assert.Equal(MyResourceText("PSP_MELDUNG_NAME_EXISTIERT"), e.Meldung);
                Assert.Equal(vorher, Anzahl(BHKWStammCtrl.TABLE));
            }
        }

        [Fact]
        public void Eine_unbekannte_Id_und_ein_leerer_Name_werden_abgelehnt()
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                int vorher = Anzahl(HeizkesselStammCtrl.TABLE);

                Katalogkopie.Ergebnis unbekannt = HeizkesselStammCtrl.Duplizieren(-1, "AD-Q11 nirgends");
                Assert.False(unbekannt.Ok);
                Assert.Equal(MyResourceText("ADM_MSG_KOPIE_FEHLT"), unbekannt.Meldung);

                DataRow original = Vorlage(HeizkesselStammCtrl.TABLE);
                Katalogkopie.Ergebnis leer = HeizkesselStammCtrl.Duplizieren(Convert.ToInt32(original["ID"]), "  ");
                Assert.False(leer.Ok);
                Assert.Equal(MyResourceText("PSP_MELDUNG_BEZEICHNER_UNGUELTIG"), leer.Meldung);

                Assert.Equal(vorher, Anzahl(HeizkesselStammCtrl.TABLE));
            }
        }

        // =================================================================================
        //  4 — Der Namensvorschlag (ohne Datenbank)
        // =================================================================================

        /// <summary>
        /// „Name (Kopie)", ist der vergeben, „Name (Kopie 2)" … — der erste freie. Die
        /// Kultur ist gepinnt: Der Zusatz ist ein Ressourcentext.
        /// </summary>
        [Fact]
        public void Der_Namensvorschlag_nimmt_den_ersten_freien_Kopienamen()
        {
            using (new Kulturvorrichtung())
            {
                var belegt = new HashSet<string> { "Kessel 1 (Kopie)", "Kessel 1 (Kopie 2)" };

                Assert.Equal("Kessel 2 (Kopie)", Katalogkopie.Namensvorschlag("Kessel 2", belegt.Contains));
                Assert.Equal("Kessel 1 (Kopie 3)", Katalogkopie.Namensvorschlag(" Kessel 1 ", belegt.Contains));
                Assert.Equal("X (Kopie)", Katalogkopie.Namensvorschlag("X", null));
            }
        }

        private static string MyResourceText(string schluessel)
            => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                   schluessel, WindowsFormsApplication1.MyResource.Resource.Culture) ?? schluessel;
    }
}
