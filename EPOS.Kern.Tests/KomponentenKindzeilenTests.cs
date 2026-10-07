using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Komponentenübernahme nimmt die KINDZEILEN der Anlage mit — Betriebsführung, Senken,
    /// Pufferverbund, Stränge, Sperrfenster — über den Kernweg
    /// <see cref="AnlagenFachspalten.AnlagenkinderUebertragen"/>, und schlüsselt ihre Verweise
    /// ins Zielprojekt um: auf die gleichnamige Gegenstelle, ohne sie leer bzw. ohne die Zeile,
    /// gemeldet als <c>BK_KOMP_HINW_KINDBEZUG</c>. Dazu der Index-Fall: Die eindeutigen Indizes
    /// <c>idx_Anlage_ID_Kessel/WP/BHKW/PUFFER</c> bringen die Übernahme nicht zu Fall.
    /// Eigene Arbeitskopie je Fall, weil jeder Fall schreibt.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KomponentenKindzeilenTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>
        /// Wärmepumpen 1043 → 1019: Das Sperrfenster kommt immer mit; ein Parallelverbund an
        /// einem Puffer mit gleichnamigem Gegenstück im Ziel zeigt auf dieses, einer ohne
        /// Gegenstück fällt weg und wird benannt.
        /// </summary>
        [Fact]
        public void Waermepumpe_bringt_Sperrfenster_und_schluesselt_den_Verbund_um()
        {
            if (!_db.Vorhanden) return;

            List<int> quellen = Anlagen(1043, WizardItemClass.WP_TYP);
            Assert.True(quellen.Count >= 1);
            int pufferA = Ganz("SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = 1043 AND Bezeichner = 'Puffer 3000Ltr'");
            int pufferB = Ganz("SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = 1043 AND Bezeichner = 'Puffer 3000Ltr (2)'");
            // Das Gegenstück von A im Ziel: der verbaute Heizungspuffer, gleich benannt.
            int zielA = Ganz("SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = 1019 AND Bezeichner = 'Vitocell 140-E 600 Ltr'");
            Ausfuehren("UPDATE Tab_Pufferspeicher SET Bezeichner = 'Puffer 3000Ltr' WHERE ID = ?", zielA);

            Ausfuehren("INSERT INTO Tab_Sperrfenster (ID_Energieanlage, Von_h, Dauer_h, Wochentage) VALUES (?, 0, 6, 31)", quellen[0]);
            Ausfuehren("INSERT INTO Z_AnlagePufferVerbund (ID_Anlage, ID_Puffer) VALUES (?, ?)", quellen[0], pufferA);
            Ausfuehren("INSERT INTO Z_AnlagePufferVerbund (ID_Anlage, ID_Puffer) VALUES (?, ?)", quellen[0], pufferB);
            string bezeichner = Text("SELECT Bezeichner FROM Tab_Energieanlagen WHERE ID = ?", quellen[0]);

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(1043, 1019, "Wärmepumpe",
                out string fehler, out string hinweise), fehler);

            int ziel = Anlagen(1019, WizardItemClass.WP_TYP)[0];
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Tab_Sperrfenster WHERE ID_Energieanlage = ? AND Dauer_h = 6 AND Wochentage = 31", ziel));
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Z_AnlagePufferVerbund WHERE ID_Anlage = ?", ziel));
            Assert.Equal(zielA, Ganz("SELECT ID_Puffer FROM Z_AnlagePufferVerbund WHERE ID_Anlage = ?", ziel));

            Assert.Contains(Vorspann(bezeichner), hinweise);
            Assert.Contains("Puffer 3000Ltr (2)", hinweise);
            // Die Quelle bleibt unberührt.
            Assert.Equal(2, Ganz("SELECT COUNT(*) FROM Z_AnlagePufferVerbund WHERE ID_Anlage = ?", quellen[0]));
        }

        /// <summary>
        /// Photovoltaik 1045 → 1007: Beide Stränge kommen mit. Der Wechselrichter zeigt auf den
        /// gleichnamigen des Ziels; das Modul des ersten Strangs auf die Gerätekopie der Anlage,
        /// das des zweiten — im Ziel ohne Gegenstück — bleibt leer und wird benannt.
        /// </summary>
        [Fact]
        public void Photovoltaik_schluesselt_Wechselrichter_und_Strangmodul_um()
        {
            if (!_db.Vorhanden) return;

            Ausfuehren("INSERT INTO Tab_Wechselrichter (ID_Projekt, Bezeichner) VALUES (1007, 'Muster 2500TL')");
            int wrZiel = Ganz("SELECT MAX(ID) FROM Tab_Wechselrichter WHERE ID_Projekt = 1007");

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(1045, 1007, "Photovoltaik",
                out string fehler, out string hinweise), fehler);

            List<int> ziele = Anlagen(1007, WizardItemClass.PV_TYP);
            Assert.Single(ziele);
            DataTable straenge = Lese("SELECT ID_Wechselrichter, ID_PV FROM Z_AnlageStrang WHERE ID_Anlage = ? ORDER BY Rang", ziele[0]);
            Assert.Equal(2, straenge.Rows.Count);
            foreach (DataRow s in straenge.Rows)
                Assert.Equal((long)wrZiel, Convert.ToInt64(s["ID_Wechselrichter"], CultureInfo.InvariantCulture));
            Assert.Equal(Ganz("SELECT ID_PV FROM Tab_Energieanlagen WHERE ID = ?", ziele[0]),
                         Convert.ToInt32(straenge.Rows[0]["ID_PV"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, straenge.Rows[1]["ID_PV"]);
            Assert.Contains("Ablytek 6MN6A290", hinweise);
            Assert.DoesNotContain("Muster 2500TL", hinweise);
        }

        /// <summary>
        /// Gegenprobe: Fehlt der Wechselrichter im Ziel, kommen die Stränge trotzdem — ohne
        /// Wechselrichter —, und der Hinweis nennt ihn.
        /// </summary>
        [Fact]
        public void Ohne_gleichnamigen_Wechselrichter_bleibt_der_Verweis_leer_und_wird_gemeldet()
        {
            if (!_db.Vorhanden) return;

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(1045, 1007, "Photovoltaik",
                out string fehler, out string hinweise), fehler);

            int ziel = Anlagen(1007, WizardItemClass.PV_TYP)[0];
            Assert.Equal(2, Ganz("SELECT COUNT(*) FROM Z_AnlageStrang WHERE ID_Anlage = ? AND ID_Wechselrichter IS NULL", ziel));
            Assert.Contains("Muster 2500TL", hinweise);
            // Kein Verweis zeigt ins Quellprojekt.
            Assert.Equal(0, Ganz("SELECT COUNT(*) FROM Z_AnlageStrang s JOIN Tab_Wechselrichter w ON w.ID = s.ID_Wechselrichter " +
                                 "WHERE s.ID_Anlage = ? AND w.ID_Projekt <> 1007", ziel));
        }

        /// <summary>
        /// Stromspeicher 1007 → 1017: Die Betriebsführung kommt vollständig mit (auch
        /// <c>PeakZiel_kW</c>), die Preisreihe zeigt auf die gleichnamige des Ziels, das
        /// Kostenprofil ohne Gegenstück bleibt leer und wird benannt, und genau eine Variante
        /// des Ziels ist aktiv — die der Quelle.
        /// </summary>
        [Fact]
        public void Stromspeicher_bringt_die_Betriebsfuehrung_umgeschluesselt_mit()
        {
            if (!_db.Vorhanden) return;

            List<int> quellen = Anlagen(1007, WizardItemClass.SP_TYP);
            Assert.True(quellen.Count >= 2);
            Ausfuehren("INSERT INTO Tab_Preisreihe (ID_Projekt, Bezeichner) VALUES (1007, 'Probe Preise')");
            int reiheQuelle = Ganz("SELECT MAX(ID) FROM Tab_Preisreihe");
            Ausfuehren("INSERT INTO Tab_Preisreihe (ID_Projekt, Bezeichner) VALUES (1017, 'Probe Preise')");
            int reiheZiel = Ganz("SELECT MAX(ID) FROM Tab_Preisreihe");
            Ausfuehren("INSERT INTO Tab_Kostenprofil (ID_Projekt, Bezeichner) VALUES (1007, 'Probe Profil')");
            int profil = Ganz("SELECT MAX(ID) FROM Tab_Kostenprofil");
            Ausfuehren("UPDATE Tab_StromspeicherVariante SET PeakZiel_kW = 42, ID_Preisreihe = ?, ID_Kostenprofil = ? " +
                       "WHERE ID_Energieanlage = ?", reiheQuelle, profil, quellen[1]);
            int aktivQuelle = quellen.FindIndex(id =>
                Ganz("SELECT COUNT(*) FROM Tab_StromspeicherVariante WHERE ID_Energieanlage = ? AND Aktiv = 1", id) > 0);
            Assert.True(aktivQuelle >= 0);

            Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(1007, 1017, "Stromspeicher",
                out string fehler, out string hinweise), fehler);

            List<int> ziele = Anlagen(1017, WizardItemClass.SP_TYP);
            Assert.Equal(quellen.Count, ziele.Count);
            DataTable v = Lese("SELECT PeakZiel_kW, ID_Preisreihe, ID_Kostenprofil FROM Tab_StromspeicherVariante WHERE ID_Energieanlage = ?", ziele[1]);
            Assert.Equal(1, v.Rows.Count);
            Assert.Equal(42.0, Convert.ToDouble(v.Rows[0]["PeakZiel_kW"], CultureInfo.InvariantCulture), 9);
            Assert.Equal((long)reiheZiel, Convert.ToInt64(v.Rows[0]["ID_Preisreihe"], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, v.Rows[0]["ID_Kostenprofil"]);
            Assert.Contains("Probe Profil", hinweise);

            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Tab_StromspeicherVariante v JOIN Tab_Energieanlagen a ON a.ID = v.ID_Energieanlage " +
                                 "WHERE a.ID_Projekt = 1017 AND v.Aktiv = 1"));
            Assert.Equal(1, Ganz("SELECT COUNT(*) FROM Tab_StromspeicherVariante WHERE ID_Energieanlage = ? AND Aktiv = 1", ziele[aktivQuelle]));
        }

        /// <summary>
        /// Index-Fall: Das Ziel führt schon einen Kessel; die Übernahme des Gewerks — auch
        /// zweimal hintereinander — scheitert nicht an <c>idx_Anlage_ID_Kessel</c>, weil sie den
        /// Kesselbestand des Ziels ersetzt und jede Anlage eine eigene Gerätekopie bekommt.
        /// Ebenso der Puffer mit <c>idx_Anlage_ID_PUFFER</c>.
        /// </summary>
        [Theory]
        [InlineData(1009, 1017, "Spitzenkessel", "ID_Kessel")]
        [InlineData(1043, 1019, "Pufferspeicher", "ID_PUFFER")]
        public void Die_eindeutigen_Geraeteindizes_bringen_die_Uebernahme_nicht_zu_Fall(int quelle, int ziel, string gewerk, string spalte)
        {
            if (!_db.Vorhanden) return;

            for (int lauf = 0; lauf < 2; lauf++)
                Assert.True(new KomponentenUebernahmeCtrl().Uebernehmen(quelle, ziel, gewerk,
                    out string fehler, out string _), fehler);

            Assert.Equal(0, Ganz("SELECT COUNT(*) FROM (SELECT [" + spalte + "] FROM Tab_Energieanlagen WHERE ID_Projekt = ? " +
                                 "AND [" + spalte + "] IS NOT NULL GROUP BY [" + spalte + "] HAVING COUNT(*) > 1)", ziel));
            Assert.Equal(Ganz("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND [" + spalte + "] IS NOT NULL", quelle),
                         Ganz("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND [" + spalte + "] IS NOT NULL", ziel));
        }

        // ------------------------------------------------------------------ Helfer

        /// <summary>Der Text des Hinweises vor der Liste der fehlenden Bezüge.</summary>
        private static string Vorspann(string anlage)
        {
            string text = string.Format(CultureInfo.CurrentCulture, Resource.BK_KOMP_HINW_KINDBEZUG, anlage, "\u0001");
            return text.Substring(0, text.IndexOf('\u0001'));
        }

        private static List<int> Anlagen(int projekt, int typ)
        {
            var liste = new List<int>();
            foreach (DataRow r in Lese("SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ? ORDER BY ID", projekt, typ).Rows)
                liste.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return liste;
        }

        private static DbParam[] Parameter(object[] werte)
        {
            var ps = new DbParam[werte.Length];
            for (int i = 0; i < werte.Length; i++) ps[i] = new DbParam("@p" + i.ToString(CultureInfo.InvariantCulture), werte[i]);
            return ps;
        }

        private static DataTable Lese(string sql, params object[] werte) => DataRepository.GetDataTable(sql, Parameter(werte));

        private static void Ausfuehren(string sql, params object[] werte) => Assert.True(DataRepository.ExecuteSQL(sql, Parameter(werte)), sql);

        private static int Ganz(string sql, params object[] werte)
        {
            object o = DataRepository.ExecuteScalar(sql, Parameter(werte));
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }

        private static string Text(string sql, params object[] werte) =>
            Convert.ToString(DataRepository.ExecuteScalar(sql, Parameter(werte)), CultureInfo.InvariantCulture);
    }
}
