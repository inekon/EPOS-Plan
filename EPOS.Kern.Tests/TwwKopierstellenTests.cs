using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die zwei Kopierstellen des Zapfprofilgenerators</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Stufe Z0, Posten P9; Abschnitt 3.2).
    ///
    /// <para><b>Duplizieren.</b> <c>Tab_TwwZone</c> und <c>Tab_TwwProjekt</c> tragen
    /// <c>ID_Projekt</c>, <c>Tab_TwwWohnungstyp</c> hängt über <c>ID_Zone</c> an der Zone.
    /// Die Kopie muss alle drei mitnehmen, die Wohnungstypen auf die NEUE Zone umhängen und
    /// die Katalogverweise (Nutzungsart, Tagesgangsatz, Bedarfstag, Ausstattungsklasse)
    /// unverändert lassen — eine benutzte Katalogzeile ist unveränderlich, die Kopie zeigt
    /// auf dieselbe Version.</para>
    ///
    /// <para><b>Transfer (.wpx).</b> Die Katalogverweise reisen über ihren natürlichen
    /// Schlüssel (<c>Bezeichner</c>, <c>Katalogversion</c>) bzw. (<c>Art</c>,
    /// <c>Schluessel</c>, <c>Katalogversion</c>). Steht die Zeile am Ziel, zeigt die Zone
    /// darauf; fehlt sie, bringt das Paket sie mit — als eigene Zeile mit
    /// <c>Status = 'IMPORT'</c>, samt Tagesgängen bzw. Ereignissen, und der Bericht nennt
    /// sie. Nie wird die Zone still auf eine fremde Zeile gleicher Id umgehängt.</para>
    ///
    /// <para>Gearbeitet wird auf einer Arbeitskopie der Testdatenbank mit ihrem fiktiven
    /// Testkatalog (Kapitel 6 (b)); Zone und Wohnungstypen tragen erfundene, runde Werte.
    /// Kein Referenzprojekt wird angefasst — Projekt 1006 ist nicht unter den fünf der
    /// CI.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class TwwKopierstellenTests
    {
        /// <summary>Ein Projekt mit Gebäude (Id 1006) — die Zone bindet sich daran.</summary>
        private const string PROJEKT = "Stromspeicher mit Wärmepumpe";

        private const string NUTZUNG = "Testnutzung A (fiktiv)";
        private const string SATZ = "Testsatz (fiktiv)";
        private const string BEDARFSTAG = "Testbedarfstag (fiktiv)";
        private const string KLASSE_A = "Testklasse A";
        private const string KLASSE_B = "Testklasse B";
        private const string VERSION = "TEST-1";

        // =============================================================================
        //  Duplizieren
        // =============================================================================
        [Fact]
        public void Duplizieren_nimmt_Zone_Wohnungstypen_und_Projektzeile_unabhaengig_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var dup = new ProjektDuplizierenCtrl();
            int quelle = dup.GetProjektId(PROJEKT);
            Assert.True(quelle > 0);
            Stand q = Anlegen(quelle);

            int neu = dup.Duplizieren(PROJEKT, "Tww Kopie");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Assert.NotEqual(quelle, neu);

            // --- Die Zone der Kopie: neue Id, dieselben Katalogverweise ----------------
            DataTable zonen = Zonen(neu);
            DataRow z = Assert.Single(zonen.Rows.Cast<DataRow>());
            long zoneNeu = Convert.ToInt64(z["ID"]);
            Assert.NotEqual(q.Zone, zoneNeu);
            Assert.Equal("Zone Probe", Convert.ToString(z["Name"]));
            Assert.Equal(12.0, Convert.ToDouble(z["Bezugsmenge"]));
            Assert.Equal(q.Nutzungsart, Convert.ToInt64(z["ID_Nutzungsart"]));
            Assert.Equal(q.Tagesgangsatz, Convert.ToInt64(z["ID_Tagesgangsatz"]));

            // Das Gebäude ist PROJEKTdatum: Die Kopie zeigt auf ihre eigene Gebäudekopie.
            long gebaeudeNeu = Convert.ToInt64(z["ID_Gebaeude"]);
            Assert.NotEqual(q.Gebaeude, gebaeudeNeu);
            Assert.Equal(neu, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Projekt FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@id", gebaeudeNeu))));

            // --- Die zwei Wohnungstypen hängen an der NEUEN Zone ----------------------
            DataTable wt = Wohnungstypen(zoneNeu);
            Assert.Equal(2, wt.Rows.Count);
            Assert.Equal(new[] { 4L, 2L }, wt.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["Anzahl"])).ToArray());
            Assert.Equal(new[] { q.KlasseA, q.KlasseB },
                         wt.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["ID_Ausstattung"])).ToArray());
            Assert.DoesNotContain(wt.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["ID"])),
                                  id => q.Wohnungstypen.Contains(id));

            // --- Die Projektzeile ------------------------------------------------------
            DataTable tp = DataRepository.GetDataTable(
                "SELECT Weg, Seed, ID_Bedarfstag FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", neu));
            DataRow p = Assert.Single(tp.Rows.Cast<DataRow>());
            Assert.Equal(TwwSchema.WEG_BESTAND, Convert.ToString(p["Weg"]));
            Assert.Equal(7L, Convert.ToInt64(p["Seed"]));
            Assert.Equal(q.Bedarfstag, Convert.ToInt64(p["ID_Bedarfstag"]));

            // --- Kein Katalog wurde vervielfältigt -------------------------------------
            Assert.Equal(q.Katalogzeilen, Katalogzeilen());

            // --- Unabhängig: Die Quelle zu löschen lässt die Kopie stehen ---------------
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_TwwZone WHERE ID = ?", new DbParam("@id", q.Zone)));
            Assert.Equal(0, Wohnungstypen(q.Zone).Rows.Count);        // Kaskade an der Quelle
            Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            Assert.Equal(2, Wohnungstypen(zoneNeu).Rows.Count);
            Assert.Equal(1L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", quelle))));
        }

        // =============================================================================
        //  Transfer (.wpx)
        // =============================================================================
        [Fact]
        public void Transfer_mit_vorhandener_Katalogzeile_zeigt_auf_dieselbe_Zeile()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);

            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            int neu = io.Importieren(paket, "Tww Rundreise", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            DataRow z = Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            Assert.NotEqual(q.Zone, Convert.ToInt64(z["ID"]));
            Assert.Equal(q.Nutzungsart, Convert.ToInt64(z["ID_Nutzungsart"]));
            Assert.Equal(q.Tagesgangsatz, Convert.ToInt64(z["ID_Tagesgangsatz"]));

            DataTable wt = Wohnungstypen(Convert.ToInt64(z["ID"]));
            Assert.Equal(new[] { q.KlasseA, q.KlasseB },
                         wt.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["ID_Ausstattung"])).ToArray());
            Assert.Equal(q.Bedarfstag, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Bedarfstag FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", neu))));

            // Nichts mitgenommen, nichts als IMPORT angelegt.
            Assert.Equal(q.Katalogzeilen, Katalogzeilen());
            Assert.Equal(0, ImportZeilen());
            Assert.DoesNotContain(io.LetzterBericht, b => b.Contains("Status IMPORT"));
        }

        [Fact]
        public void Transfer_mit_fehlender_Katalogzeile_bringt_sie_als_IMPORT_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);

            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            // Am "Ziel" fehlen jetzt die Zeilen unter ihrem natürlichen Schlüssel: Die
            // Katalogversion der vier Zeilen wandert weg. Ihre Ids bleiben belegt — genau
            // die Falle, in der ein Transfer über die Id still auf eine FREMDE Zeile
            // umhinge.
            Umversionieren(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, q.Nutzungsart);
            Umversionieren(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, q.Tagesgangsatz);
            Umversionieren(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, q.Bedarfstag);
            Umversionieren(TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, q.KlasseA);
            int vorher = Katalogzeilen();

            int neu = io.Importieren(paket, "Tww Mitnahme", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            DataRow z = Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            long nutzung = Convert.ToInt64(z["ID_Nutzungsart"]);
            long satz = Convert.ToInt64(z["ID_Tagesgangsatz"]);
            Assert.NotEqual(q.Nutzungsart, nutzung);
            Assert.NotEqual(q.Tagesgangsatz, satz);

            // --- Die mitgenommene Nutzungsart: Status IMPORT, beschreibbar, eigener Satz --
            DataRow n = Assert.Single(DataRepository.GetDataTable(
                "SELECT * FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", nutzung)).Rows.Cast<DataRow>());
            Assert.Equal(NUTZUNG, Convert.ToString(n["Bezeichner"]));
            Assert.Equal(VERSION, Convert.ToString(n["Katalogversion"]));
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(n["Status"]));
            Assert.Equal(0L, Convert.ToInt64(n["ReadOnly"]));
            Assert.Equal(DBNull.Value, n["ID_Vorlage"]);
            Assert.Equal(TwwSchema.HERKUNFT_FIKTIV, Convert.ToString(n["Bedarf_Herkunftsart"]));
            Assert.Equal(satz, Convert.ToInt64(n["ID_Tagesgangsatz"]));

            // --- Der Tagesgangsatz reist mit seinen vier Tagesgängen -------------------
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Status FROM Tab_TwwTagesgangsatz_STAMM WHERE ID = ?", new DbParam("@id", satz))));
            Assert.Equal(4L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = ?", new DbParam("@id", satz))));
            Assert.Equal(0.25, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Anteil_07 FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = ? AND Tagtyp = 1",
                new DbParam("@id", satz))));

            // --- Der Bedarfstag mit seinen drei Ereignissen ----------------------------
            long tag = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Bedarfstag FROM Tab_TwwProjekt WHERE ID_Projekt = ?", new DbParam("@p", neu)));
            Assert.NotEqual(q.Bedarfstag, tag);
            Assert.Equal(3L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_TwwBedarfstagEreignis_STAMM WHERE ID_Bedarfstag = ?", new DbParam("@id", tag))));

            // --- Ausstattung: A fehlte und kam mit, B stand am Ziel ---------------------
            DataTable wt = Wohnungstypen(Convert.ToInt64(z["ID"]));
            long[] klassen = wt.Rows.Cast<DataRow>().Select(r => Convert.ToInt64(r["ID_Ausstattung"])).ToArray();
            Assert.NotEqual(q.KlasseA, klassen[0]);
            Assert.Equal(q.KlasseB, klassen[1]);
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Status FROM Tab_TwwDin4708Wert_STAMM WHERE ID = ?", new DbParam("@id", klassen[0]))));

            // Vier Köpfe, vier Tagesgänge, drei Ereignisse — und der Bericht nennt die Köpfe.
            Assert.Equal(vorher + 4 + 4 + 3, Katalogzeilen());
            Assert.Equal(4, ImportZeilen());
            Assert.Equal(4, io.LetzterBericht.Count(b => b.Contains("Status IMPORT")));

            // --- Ein zweiter Import findet die mitgenommenen Zeilen wieder ---------------
            int zweit = io.Importieren(paket, "Tww Mitnahme", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                       null, out fehler);
            Assert.True(zweit > 0, "Zweiter Import fehlgeschlagen: " + fehler);
            Assert.Equal(vorher + 11, Katalogzeilen());
            DataRow z2 = Assert.Single(Zonen(zweit).Rows.Cast<DataRow>());
            Assert.Equal(nutzung, Convert.ToInt64(z2["ID_Nutzungsart"]));
        }

        // =============================================================================
        //  Handwerkszeug
        // =============================================================================

        private sealed class Stand
        {
            internal long Nutzungsart, Tagesgangsatz, Bedarfstag, KlasseA, KlasseB, Gebaeude, Zone;
            internal List<long> Wohnungstypen = new List<long>();
            internal int Katalogzeilen;
        }

        /// <summary>
        /// Legt im Projekt eine Zone (auf das Gebäude des Projekts), zwei Wohnungstypen und
        /// die Projektzeile an — erfundene, runde Werte auf dem fiktiven Testkatalog.
        /// </summary>
        private static Stand Anlegen(int idProjekt)
        {
            var s = new Stand
            {
                Nutzungsart = Katalog(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, "Bezeichner", NUTZUNG),
                Tagesgangsatz = Katalog(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, "Bezeichner", SATZ),
                Bedarfstag = Katalog(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, "Bezeichner", BEDARFSTAG),
                KlasseA = Katalog(TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, "Schluessel", KLASSE_A),
                KlasseB = Katalog(TwwSchema.TAB_TWW_DIN4708_WERT_STAMM, "Schluessel", KLASSE_B),
                Gebaeude = Convert.ToInt64(DataRepository.ExecuteScalar(
                    "SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", idProjekt)))
            };

            s.Zone = DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_TwwZone (ID_Projekt, ID_Nutzungsart, ID_Tagesgangsatz, ID_Gebaeude, Reihenfolge, " +
                "Name, Bezugsmenge, Niveau) VALUES (?, ?, ?, ?, 1, 'Zone Probe', 12.0, 3)",
                new[]
                {
                    new DbParam("@p", idProjekt), new DbParam("@n", s.Nutzungsart),
                    new DbParam("@t", s.Tagesgangsatz), new DbParam("@g", s.Gebaeude)
                });
            s.Wohnungstypen.Add(DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_TwwWohnungstyp (ID_Zone, Anzahl, Raumzahl, ID_Ausstattung, Reihenfolge) " +
                "VALUES (?, 4, 3.0, ?, 1)",
                new[] { new DbParam("@z", s.Zone), new DbParam("@a", s.KlasseA) }));
            s.Wohnungstypen.Add(DataRepository.ExecuteInsertAndGetId(
                "INSERT INTO Tab_TwwWohnungstyp (ID_Zone, Anzahl, Raumzahl, ID_Ausstattung, Reihenfolge) " +
                "VALUES (?, 2, 2.0, ?, 2)",
                new[] { new DbParam("@z", s.Zone), new DbParam("@a", s.KlasseB) }));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_TwwProjekt (ID_Projekt, Weg, Seed, ID_Bedarfstag) VALUES (?, 'BESTAND', 7, ?)",
                new DbParam("@p", idProjekt), new DbParam("@b", s.Bedarfstag)));

            s.Katalogzeilen = Katalogzeilen();
            return s;
        }

        private static long Katalog(string tabelle, string spalte, string wert)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT ID FROM \"" + tabelle + "\" WHERE \"" + spalte + "\" = ? AND Katalogversion = ?",
                new DbParam("@w", wert), new DbParam("@k", VERSION));
            Assert.True(o != null && o != DBNull.Value, tabelle + ": " + wert + " fehlt im Testkatalog.");
            return Convert.ToInt64(o);
        }

        private static void Umversionieren(string tabelle, long id) =>
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE \"" + tabelle + "\" SET Katalogversion = 'TEST-ANDERS' WHERE ID = ?", new DbParam("@id", id)));

        private static DataTable Zonen(long idProjekt) => DataRepository.GetDataTable(
            "SELECT * FROM Tab_TwwZone WHERE ID_Projekt = ? ORDER BY Reihenfolge", new DbParam("@p", idProjekt));

        private static DataTable Wohnungstypen(long idZone) => DataRepository.GetDataTable(
            "SELECT * FROM Tab_TwwWohnungstyp WHERE ID_Zone = ? ORDER BY Reihenfolge", new DbParam("@z", idZone));

        private static readonly string[] KATALOGE =
        {
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_TAGESGANG_STAMM, TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM, TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_DIN4708_WERT_STAMM
        };

        private static int Katalogzeilen() =>
            KATALOGE.Sum(t => Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM \"" + t + "\"")));

        private static int ImportZeilen()
        {
            int n = 0;
            foreach (string t in KATALOGE)
                if (DataRepository.SpalteVorhanden(t, "Status"))
                    n += Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM \"" + t + "\" WHERE Status = ?", new DbParam("@s", TwwSchema.STATUS_IMPORT)));
            return n;
        }

        /// <summary>Ein Ordner für die Paketdateien einer Probe; er räumt sich selbst auf.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad = Path.Combine(Path.GetTempPath(),
                "epos-twwtransfer-" + Guid.NewGuid().ToString("N").Substring(0, 8));

            public Arbeitsordner() { Directory.CreateDirectory(_pfad); }

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufräumen darf nicht scheitern */ }
            }
        }
    }
}
