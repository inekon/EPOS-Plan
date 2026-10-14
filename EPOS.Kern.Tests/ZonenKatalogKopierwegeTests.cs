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
    /// Die Kopierwege der <b>Zonen im Gebäudekatalog</b> (Schritt ZK, <see cref="Zonenkopie"/>) mit einem
    /// synthetischen Katalogsatz: zwei Zonen, ein Bauteil mit Nachbarzone und Trennfläche, ein Außenbauteil, ein
    /// Luftstrom, Konditionierung an beiden Zonen und am Katalogbau. Katalog → Projekt (Übernahme), Projekt → Katalog
    /// („In DB übernehmen", „Speichern unter"), Duplizieren, Löschen, und dass die Zonenzeilen der Konditionierung
    /// nie in die Ebene des Katalogbaus geraten (Leser, Prüfsumme, Kopie).
    /// </summary>
    [Collection("Testdatenbank")]
    public class ZonenKatalogKopierwegeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private bool Bereit() => _db.Vorhanden && ZonenKatalogSchema.Lesbar() && KonditionierungSchema.Lesbar();

        /// <summary>Die Ids des synthetischen Satzes.</summary>
        private sealed record Satz(long Stamm, long Nord, long Sued);

        [Fact]
        public void Die_Uebernahme_ins_Projekt_kopiert_Zonen_Bauteile_Luftstroeme_und_Zonen_Konditionierung()
        {
            if (!Bereit()) return;
            Satz s = SatzAnlegen("ZK Probe Übernahme");

            int idGebaeude = Uebernehmen(s.Stamm, out _);
            Assert.True(idGebaeude > 0);

            List<(long Id, string Name)> zonen = Zonen("Tab_Zone", idGebaeude);
            Assert.Equal(new[] { "Nord", "Süd" }, zonen.Select(z => z.Name).ToArray());
            long nord = zonen[0].Id, sued = zonen[1].Id;
            PruefeBauteileUndLuftstrom("Tab_Bauteil", "Tab_Zonenluftstrom", nord, sued);

            // Die Zone der Projektkopie rechnet nach dem Zonenmodell: der Leser des Laufs sieht beide.
            Assert.Equal(2, new GebaeudeZonenCtrl().LesenJeGebaeude(idGebaeude).Count);

            // Konditionierung: je Zone ihre Zeilen, die Gebaeudeebene genau die des Katalogbaus.
            var ctrl = new KonditionierungCtrl();
            Assert.Equal(21.0, Wert("SELECT \"Wert\" FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone\" = ?", nord));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\" p INNER JOIN \"Tab_Konditionierungskalender\" k " +
                                  "ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Zone\" = ?", nord));
            Assert.Single(ctrl.Vorgaben(KonditionierungCtrl.Eigner.Zone(idGebaeude, sued)));
            Assert.Equal(19.0, Wert("SELECT \"Wert\" FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL",
                                    idGebaeude));
            Assert.Empty(ctrl.Vorgaben(KonditionierungCtrl.Eigner.Gebaeude(idGebaeude)));
        }

        [Fact]
        public void In_DB_uebernehmen_kopiert_die_Zonen_des_Projektgebaeudes_in_den_Katalog()
        {
            if (!Bereit()) return;
            Satz s = SatzAnlegen("ZK Probe Rückweg");
            Uebernehmen(s.Stamm, out long z);

            GebaeudeStammCtrl.ProjektuebernahmeErgebnis e = GebaeudeStammCtrl.AusProjektUebernehmen((int)z, "ZK Probe Rückweg (2)");
            Assert.True(e.Ok, e.Meldung);
            Assert.Equal(2, e.Zonen);
            Assert.Equal(2, e.Bauteile);
            PruefeKatalogsatz(e.Id);
        }

        [Fact]
        public void Speichern_unter_nimmt_die_Zonen_der_Quelle_mit()
        {
            if (!Bereit()) return;
            Satz s = SatzAnlegen("ZK Probe Speichern");
            int idGebaeude = Uebernehmen(s.Stamm, out _);

            GebaeudeModel kopf = new GebaeudeStammCtrl().Lies("ZK Probe Speichern");
            kopf.Gebaeudename = "ZK Probe Speichern (Projekt)";
            GebaeudeStammCtrl.SpeichernUnterErgebnis ausProjekt =
                GebaeudeStammCtrl.SpeichernUnter(kopf, KonditionierungCtrl.Eigner.Gebaeude(idGebaeude));
            Assert.True(ausProjekt.Ok, ausProjekt.Meldung);
            PruefeKatalogsatz(ausProjekt.Id);

            kopf.Gebaeudename = "ZK Probe Speichern (Katalog)";
            GebaeudeStammCtrl.SpeichernUnterErgebnis ausKatalog =
                GebaeudeStammCtrl.SpeichernUnter(kopf, KonditionierungCtrl.Eigner.Katalogbau(s.Stamm));
            Assert.True(ausKatalog.Ok, ausKatalog.Meldung);
            PruefeKatalogsatz(ausKatalog.Id);
        }

        [Fact]
        public void Duplizieren_dupliziert_die_Zonen_und_Loeschen_raeumt_sie_ab()
        {
            if (!Bereit()) return;
            Satz s = SatzAnlegen("ZK Probe Duplikat");

            Katalogkopie.Ergebnis d = GebaeudeStammCtrl.Duplizieren((int)s.Stamm, "ZK Probe Duplikat (2)");
            Assert.True(d.Ok, d.Meldung);
            PruefeKatalogsatz(d.Id);
            Assert.Equal(2, Zonenkopie.Anzahl(Zonenebene.Katalog, s.Stamm));

            Assert.True(GebaeudeStammCtrl.Loeschen("ZK Probe Duplikat (2)"));
            Assert.Equal(0, Zonenkopie.Anzahl(Zonenebene.Katalog, d.Id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude_Stamm\" = ?", d.Id));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM \"Tab_Bauteil_STAMM\" b LEFT JOIN \"Tab_Zone_STAMM\" z ON z.\"ID\" = b.\"ID_Zone\" " +
                                  "WHERE z.\"ID\" IS NULL"));
            Assert.Equal(2, Zonenkopie.Anzahl(Zonenebene.Katalog, s.Stamm));
        }

        /// <summary>
        /// Die Zonenzeilen der Konditionierung gehören nicht zur Ebene des Katalogbaus: Leser, Zählung je Katalogbau
        /// und Kindzeilen der Katalogfassung (Prüfsumme, Paket) sehen nur die Gebäudeebene.
        /// </summary>
        [Fact]
        public void Die_Ebene_des_Katalogbaus_sieht_keine_Zonenzeilen()
        {
            if (!Bereit()) return;
            Satz s = SatzAnlegen("ZK Probe Ebene");
            var ctrl = new KonditionierungCtrl();

            Assert.Empty(ctrl.Vorgaben(KonditionierungCtrl.Eigner.Katalogbau(s.Stamm)));
            Assert.Single(ctrl.Vorgaben(KonditionierungCtrl.Eigner.Katalogzone(s.Stamm, s.Sued)));
            Assert.Equal(1, Konditionierungdatenweg.KalenderJeKatalogbau()[s.Stamm]);

            Katalogtabelle t = Katalogfassung.Tabelle("Tab_Gebaeude_STAMM");
            var kinder = Katalogfassung.Kinder(t, s.Stamm, Katalogfassung.LeseOhneVorgang);
            Assert.Single(kinder["Tab_Konditionierungskalender"]);
            Assert.Empty(kinder["Tab_Konditionierungsvorgabe"]);
        }

        // =============================================================================
        //  Aufbau und Prüfung
        // =============================================================================

        /// <summary>
        /// Ein Anwendersatz (Duplikat des ersten Katalogsatzes) mit zwei Zonen: „Nord" trägt eine Innenwand an „Süd"
        /// (Trennfläche IW), „Süd" eine Außenwand; ein Luftstrom; ein Heizkalender an „Nord" mit einer Periode, eine
        /// Vorgabe an „Süd", ein Heizkalender am Katalogbau.
        /// </summary>
        private static Satz SatzAnlegen(string name)
        {
            long quelle = Zahl("SELECT MIN(ID) FROM \"Tab_Gebaeude_STAMM\"");
            Katalogkopie.Ergebnis k = GebaeudeStammCtrl.Duplizieren((int)quelle, name);
            Assert.True(k.Ok, k.Meldung);
            long stamm = k.Id;
            // Das Duplikat traegt die Konditionierung der Quelle; der Satz beginnt leer.
            DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude_Stamm\" = ?", new DbParam("@s", stamm));
            DataRepository.ExecuteSQL("DELETE FROM \"Tab_Konditionierungsvorgabe\" WHERE \"ID_Gebaeude_Stamm\" = ?", new DbParam("@s", stamm));
            DataRepository.ExecuteSQL("DELETE FROM \"Tab_Zone_STAMM\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@s", stamm));

            long nord = Zone(stamm, 1, "Nord");
            long sued = Zone(stamm, 2, "Süd");
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Bauteil_STAMM\" (\"ID_Zone\", \"Rang\", \"Bezeichner\", \"Bauteilart\", \"Flaeche\", \"U_Wert\", " +
                "\"Randbedingung\", \"Psi_L\", \"ID_Nachbarzone\", \"Trennflaeche_Zuordnung\") VALUES (?, 1, 'Trennwand', 'INNENWAND', 12.5, 1.2, " +
                "'ZONE', 0.1, ?, 'IW')", new DbParam("@z", nord), new DbParam("@n", sued)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Bauteil_STAMM\" (\"ID_Zone\", \"Rang\", \"Bezeichner\", \"Bauteilart\", \"Flaeche\", \"U_Wert\", " +
                "\"Neigung\", \"Azimut\", \"Randbedingung\") VALUES (?, 1, 'Südwand', 'AUSSENWAND', 30.0, 0.28, 90.0, 180.0, 'AUSSENLUFT')",
                new DbParam("@z", sued)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zonenluftstrom_STAMM\" (\"ID_ZoneA\", \"ID_ZoneB\", \"Volumenstrom\") VALUES (?, ?, 50.0)",
                new DbParam("@a", Math.Min(nord, sued)), new DbParam("@b", Math.Max(nord, sued))));

            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Konditionierungskalender\" (\"ID_Gebaeude_Stamm\", \"Groesse\", \"Wert\") VALUES (?, 'HEIZSOLL', 19.0)",
                new DbParam("@s", stamm)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Konditionierungskalender\" (\"ID_Gebaeude_Stamm\", \"ID_Zone_Stamm\", \"Groesse\", \"Wert\") " +
                "VALUES (?, ?, 'HEIZSOLL', 21.0)", new DbParam("@s", stamm), new DbParam("@z", nord)));
            long kalender = Zahl("SELECT MAX(ID) FROM \"Tab_Konditionierungskalender\"");
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Konditionierungsperiode\" (\"ID_Kalender\", \"Rang\", \"Art\", \"Bezeichner\", \"Beginn\", \"Ende\", \"Wert\") " +
                "VALUES (?, 400, 'ZEITRAUM', 'Probe', 10, 20, 18.0)", new DbParam("@k", kalender)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Konditionierungsvorgabe\" (\"ID_Gebaeude_Stamm\", \"ID_Zone_Stamm\", \"Groesse\", \"Zeile\", \"Wert\") " +
                "VALUES (?, ?, 'HEIZSOLL', 'TAG', 20.0)", new DbParam("@s", stamm), new DbParam("@z", sued)));
            return new Satz(stamm, nord, sued);
        }

        private static long Zone(long stamm, int rang, string name)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO \"Tab_Zone_STAMM\" (\"ID_Gebaeude\", \"Rang\", \"Bezeichner\", \"Nutzflaeche\", \"Raumhoehe\", \"IstBeheizt\", " +
                "\"Raumsolltemperatur_Tag\") VALUES (?, ?, ?, 100.0, 3.0, 1, 21.0)",
                new DbParam("@g", stamm), new DbParam("@r", rang), new DbParam("@b", name)));
            return Zahl("SELECT MAX(ID) FROM \"Tab_Zone_STAMM\"");
        }

        /// <summary>Ein Katalogsatz trägt die Zonen, Bauteile, den Luftstrom und die Zonen-Konditionierung des Musters.</summary>
        private static void PruefeKatalogsatz(long stamm)
        {
            List<(long Id, string Name)> zonen = Zonen("Tab_Zone_STAMM", stamm);
            Assert.Equal(new[] { "Nord", "Süd" }, zonen.Select(z => z.Name).ToArray());
            long nord = zonen[0].Id, sued = zonen[1].Id;
            PruefeBauteileUndLuftstrom("Tab_Bauteil_STAMM", "Tab_Zonenluftstrom_STAMM", nord, sued);
            Assert.Equal(21.0, Wert("SELECT \"Wert\" FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Zone_Stamm\" = ?", nord));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"Tab_Konditionierungsperiode\" p INNER JOIN \"Tab_Konditionierungskalender\" k " +
                                  "ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Zone_Stamm\" = ?", nord));
            Assert.Single(new KonditionierungCtrl().Vorgaben(KonditionierungCtrl.Eigner.Katalogzone(stamm, sued)));
            Assert.Equal(19.0, Wert("SELECT \"Wert\" FROM \"Tab_Konditionierungskalender\" WHERE \"ID_Gebaeude_Stamm\" = ? " +
                                    "AND \"ID_Zone_Stamm\" IS NULL", stamm));
        }

        /// <summary>Die Innenwand von „Nord" zeigt auf das neue „Süd", die Außenwand steht an „Süd", ein Luftstrom verbindet beide.</summary>
        private static void PruefeBauteileUndLuftstrom(string bauteil, string luftstrom, long nord, long sued)
        {
            DataTable trenn = DataRepository.GetDataTable(
                "SELECT \"ID_Nachbarzone\", \"Trennflaeche_Zuordnung\", \"Flaeche\", \"Psi_L\" FROM \"" + bauteil + "\" WHERE \"ID_Zone\" = ?",
                new DbParam("@z", nord));
            Assert.Single(trenn.Rows.Cast<DataRow>());
            Assert.Equal(sued, Convert.ToInt64(trenn.Rows[0][0], CultureInfo.InvariantCulture));
            Assert.Equal("IW", Convert.ToString(trenn.Rows[0][1], CultureInfo.InvariantCulture));
            Assert.Equal(12.5, Convert.ToDouble(trenn.Rows[0][2], CultureInfo.InvariantCulture));
            Assert.Equal(0.1, Convert.ToDouble(trenn.Rows[0][3], CultureInfo.InvariantCulture));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"" + bauteil + "\" WHERE \"ID_Zone\" = ? AND \"Bauteilart\" = 'AUSSENWAND' " +
                                  "AND \"Azimut\" = 180.0", sued));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM \"" + luftstrom + "\" WHERE \"ID_ZoneA\" = ? AND \"ID_ZoneB\" = ? AND \"Volumenstrom\" = 50.0",
                                  Math.Min(nord, sued), Math.Max(nord, sued)));
        }

        private static List<(long Id, string Name)> Zonen(string tabelle, long idGebaeude)
            => DataRepository.GetDataTable("SELECT \"ID\", \"Bezeichner\" FROM \"" + tabelle + "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"Rang\"",
                                           new DbParam("@g", idGebaeude))
                   .Rows.Cast<DataRow>()
                   .Select(r => (Convert.ToInt64(r[0], CultureInfo.InvariantCulture), Convert.ToString(r[1], CultureInfo.InvariantCulture)))
                   .ToList();

        /// <summary>Die Übernahme ins erste Projekt über eine neue Zuordnungszeile (wie der Assistent).</summary>
        private static int Uebernehmen(long stamm, out long z)
        {
            long projekt = Zahl("SELECT MIN(ID) FROM \"Tab_Projekt\"");
            DataRepository.ExecuteSQL(
                "INSERT INTO \"Z_ProjektGebaeude\" (\"ID_Projekt\", \"Wohnflaeche_Waermebedarf\", " +
                "\"Einheit_Waermebedarf_Wohnflaeche\", \"Jahresnutzungsgrad\", \"dezWarmwasserbereitung\") VALUES (?, 100.0, ?, 0.9, 0)",
                new DbParam("@p", projekt), new DbParam("@e", "m2"));
            z = Zahl("SELECT MAX(ID) FROM \"Z_ProjektGebaeude\"");
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT \"Bezeichner\" FROM \"Tab_Gebaeude_STAMM\" WHERE \"ID\" = ?",
                                                                        new DbParam("@s", stamm)), CultureInfo.InvariantCulture);
            return new GebaeudeStammCtrl().CopyFromStamm((int)stamm, name, (int)projekt, (int)z);
        }

        private static long Zahl(string sql, params object[] p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray()),
                               CultureInfo.InvariantCulture);

        private static double Wert(string sql, long id)
            => Convert.ToDouble(DataRepository.ExecuteScalar(sql, new DbParam("@id", id)), CultureInfo.InvariantCulture);
    }
}
