using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json.Nodes;
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

        /// <summary>
        /// Die benannte Ablehnung: Die Nutzungsart fehlt am Ziel, ihr Tagesgangsatz steht nicht
        /// im Paket (ein Paket, das nicht dieser Weg geschrieben hat). Der Import bricht mit
        /// Namen ab und ändert nichts — kein Projekt, keine Zone, keine Katalogzeile.
        /// </summary>
        [Fact]
        public void Transfer_ohne_Tagesgangsatz_im_Paket_wird_benannt_abgelehnt_und_aendert_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);

            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));
            KatalogAusPaketEntfernen(paket, TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM);

            Umversionieren(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, q.Nutzungsart);
            (int Projekte, int Zonen, int Katalog) vorher = Bestand();

            int neu = io.Importieren(paket, "Tww Ablehnung", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu <= 0, "Der Import haette abgelehnt werden muessen.");
            Assert.Contains(NUTZUNG, fehler);
            Assert.Contains("Import abgelehnt", fehler);
            Assert.Equal(vorher, Bestand());
            Assert.Equal(0, ImportZeilen());
        }

        /// <summary>
        /// Der Mischfall: Nur die Nutzungsart fehlt am Ziel, ihr Tagesgangsatz steht dort. Die
        /// mitgenommene Nutzungsart zeigt auf den GEFUNDENEN Satz; kein neuer Satz, kein neuer
        /// Tagesgang.
        /// </summary>
        [Fact]
        public void Transfer_mit_fehlender_Nutzungsart_und_vorhandenem_Satz_zeigt_auf_den_Satz()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);

            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            Umversionieren(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, q.Nutzungsart);
            int vorher = Katalogzeilen();

            int neu = io.Importieren(paket, "Tww Mischfall", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            DataRow z = Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            long nutzung = Convert.ToInt64(z["ID_Nutzungsart"]);
            Assert.NotEqual(q.Nutzungsart, nutzung);
            Assert.Equal(q.Tagesgangsatz, Convert.ToInt64(z["ID_Tagesgangsatz"]));
            Assert.Equal(q.Tagesgangsatz, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", nutzung))));

            Assert.Equal(vorher + 1, Katalogzeilen());
            Assert.Equal(1, ImportZeilen());
            Assert.Equal(1, io.LetzterBericht.Count(b => b.Contains("Status IMPORT")));
        }

        /// <summary>
        /// Mitnahme nur bei Bedarf: Die Zone zeigt NICHT direkt auf einen Tagesgangsatz. Fehlt
        /// am Ziel nur der Satz, die Nutzungsart aber steht dort, bleibt der Satz liegen — er
        /// verwiese auf nichts. Fehlen beide, holt die mitgenommene Nutzungsart ihn nach.
        /// </summary>
        [Fact]
        public void Transfer_nimmt_einen_nur_abhaengigen_Tagesgangsatz_nur_bei_Bedarf_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwZone SET ID_Tagesgangsatz = NULL WHERE ID = ?", new DbParam("@id", q.Zone)));

            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            // --- Nur der Satz fehlt: nichts wird mitgenommen ----------------------------
            Umversionieren(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, q.Tagesgangsatz);
            int vorher = Katalogzeilen();
            int neu = io.Importieren(paket, "Tww ohne Bedarf", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
            DataRow z = Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            Assert.Equal(q.Nutzungsart, Convert.ToInt64(z["ID_Nutzungsart"]));
            Assert.Equal(DBNull.Value, z["ID_Tagesgangsatz"]);
            Assert.Equal(vorher, Katalogzeilen());
            Assert.Equal(0, ImportZeilen());
            Assert.DoesNotContain(io.LetzterBericht, b => b.Contains("Status IMPORT"));

            // --- Beide fehlen: die Nutzungsart holt ihren Satz samt Tagesgängen ----------
            Umversionieren(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, q.Nutzungsart);
            int zweit = io.Importieren(paket, "Tww mit Bedarf", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                       null, out fehler);
            Assert.True(zweit > 0, "Import fehlgeschlagen: " + fehler);
            long nutzung = Convert.ToInt64(Assert.Single(Zonen(zweit).Rows.Cast<DataRow>())["ID_Nutzungsart"]);
            long satz = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", nutzung)));
            Assert.NotEqual(q.Tagesgangsatz, satz);
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Status FROM Tab_TwwTagesgangsatz_STAMM WHERE ID = ?", new DbParam("@id", satz))));
            Assert.Equal(4L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = ?", new DbParam("@id", satz))));
            Assert.Equal(vorher + 1 + 1 + 4, Katalogzeilen());
            Assert.Equal(2, ImportZeilen());
        }

        /// <summary>
        /// Eine Zone zeigt auf eine Tww-Katalogzeile, die das Paket nicht führt: Der Import
        /// hängt sie nicht still über die Original-Id um, sondern lehnt benannt ab.
        /// </summary>
        [Fact]
        public void Transfer_ohne_Katalogzeile_im_Paket_wird_benannt_abgelehnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Anlegen(quelle);

            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));
            KatalogAusPaketEntfernen(paket, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM);
            (int Projekte, int Zonen, int Katalog) vorher = Bestand();

            int neu = io.Importieren(paket, "Tww Fremdverweis", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu <= 0, "Der Import haette abgelehnt werden muessen.");
            Assert.Contains("ID_Nutzungsart", fehler);
            Assert.Contains(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ", die das Paket nicht führt", fehler);
            Assert.Equal(vorher, Bestand());
        }

        /// <summary>
        /// <c>Beleg</c> (interne Sekundärquelle, Konzept 6 (e)) und <c>Freigabe</c> reisen nicht
        /// ins Paket — es geht an Dritte.
        /// </summary>
        [Fact]
        public void Das_Paket_fuehrt_weder_Beleg_noch_Freigabe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwNutzungsart_STAMM SET Beleg = 'intern (Probe)', Freigabe = 'Probe' WHERE ID = ?",
                new DbParam("@id", q.Nutzungsart)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwDin4708Wert_STAMM SET Beleg = 'intern (Probe)' WHERE ID = ?", new DbParam("@id", q.KlasseA)));

            string paket = ordner.Datei("tww.wpx");
            Assert.True(new ProjektExportImportCtrl().Exportieren(PROJEKT, paket));

            using ZipArchive zip = ZipFile.OpenRead(paket);
            var tww = zip.Entries.Where(e => e.FullName.StartsWith("catalog", StringComparison.Ordinal) &&
                                             e.FullName.Contains("Tab_Tww")).ToList();
            Assert.Contains(tww, e => e.FullName == "catalogs/" + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".json");
            foreach (ZipArchiveEntry e in tww)
            {
                using var r = new StreamReader(e.Open());
                string json = r.ReadToEnd();
                Assert.DoesNotContain("\"Beleg\"", json);
                Assert.DoesNotContain("\"Freigabe\"", json);
                Assert.DoesNotContain("intern (Probe)", json);
            }
        }

        // =============================================================================
        //  ZU17 — Inhaltsvergleich namensgleicher EIGEN-Zeilen (N6)
        // =============================================================================

        /// <summary>
        /// Gleicher Inhalt: Die namensgleiche EIGEN-Zeile am Ziel trägt dieselben Werte — nur ihre
        /// Provenienz (Quelle) ist anders. Die Zone zeigt auf sie, nichts wird mitgenommen, der
        /// Bericht nennt keine Abweichung.
        /// </summary>
        [Fact]
        public void Transfer_mit_namensgleicher_Zeile_gleichen_Inhalts_zeigt_auf_sie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);
            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwNutzungsart_STAMM SET Bedarf_Quelle = 'Andere Quelle (fiktiv)' WHERE ID = ?",
                new DbParam("@id", q.Nutzungsart)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwTagesgang_STAMM SET Quelle = 'Andere Quelle (fiktiv)' WHERE ID_Tagesgangsatz = ?",
                new DbParam("@id", q.Tagesgangsatz)));
            int vorher = Katalogzeilen();

            int neu = io.Importieren(paket, "Tww Gleich", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            DataRow z = Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            Assert.Equal(q.Nutzungsart, Convert.ToInt64(z["ID_Nutzungsart"]));
            Assert.Equal(q.Tagesgangsatz, Convert.ToInt64(z["ID_Tagesgangsatz"]));
            Assert.Equal(vorher, Katalogzeilen());
            Assert.Equal(0, ImportZeilen());
            Assert.DoesNotContain(io.LetzterBericht, b => b.Contains("weicht"));
        }

        /// <summary>
        /// Abweichender Inhalt: Am Ziel trägt die namensgleiche EIGEN-Nutzungsart einen anderen
        /// Bedarf und die Ausstattungsklasse A einen anderen Wert. Beide kommen als NEUE Version
        /// mit dem Zusatz „ (Import 1)“ und Status IMPORT; die Zielzeilen bleiben unverändert, der
        /// gleiche Tagesgangsatz wird weiter benutzt, der Bericht nennt beides. Ein zweiter Import
        /// findet die mitgenommene Version wieder, statt eine dritte anzulegen.
        /// </summary>
        [Fact]
        public void Transfer_mit_abweichender_Zeile_nimmt_eine_neue_Version_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);
            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            // Das Ziel ändert seine EIGEN-Zeilen unter demselben Namen (erfundene Werte).
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwNutzungsart_STAMM SET Bedarf_Mittel = 2.5 WHERE ID = ?", new DbParam("@id", q.Nutzungsart)));
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwDin4708Wert_STAMM SET Wert = 15.0 WHERE ID = ?", new DbParam("@id", q.KlasseA)));
            int vorher = Katalogzeilen();

            int neu = io.Importieren(paket, "Tww Abweichend", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            DataRow z = Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            long nutzung = Convert.ToInt64(z["ID_Nutzungsart"]);
            Assert.NotEqual(q.Nutzungsart, nutzung);
            DataRow n = Assert.Single(DataRepository.GetDataTable(
                "SELECT * FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", nutzung)).Rows.Cast<DataRow>());
            Assert.Equal(NUTZUNG + " (Import 1)", Convert.ToString(n["Bezeichner"]));
            Assert.Equal(VERSION, Convert.ToString(n["Katalogversion"]));
            Assert.Equal(TwwSchema.STATUS_IMPORT, Convert.ToString(n["Status"]));
            Assert.Equal(2.0, Convert.ToDouble(n["Bedarf_Mittel"]));                       // der Wert des Pakets
            Assert.Equal(q.Tagesgangsatz, Convert.ToInt64(n["ID_Tagesgangsatz"]));        // Satz gleich: weiter benutzt
            Assert.Equal(q.Tagesgangsatz, Convert.ToInt64(z["ID_Tagesgangsatz"]));

            // Die Zielzeilen bleiben unberührt.
            Assert.Equal(2.5, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Bedarf_Mittel FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", q.Nutzungsart))));
            Assert.Equal(TwwSchema.STATUS_EIGEN, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Status FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", q.Nutzungsart))));

            // Ausstattungsklasse A als neue Version im Schlüssel, B unverändert zugeordnet.
            long[] klassen = Wohnungstypen(Convert.ToInt64(z["ID"])).Rows.Cast<DataRow>()
                .Select(r => Convert.ToInt64(r["ID_Ausstattung"])).ToArray();
            Assert.NotEqual(q.KlasseA, klassen[0]);
            Assert.Equal(q.KlasseB, klassen[1]);
            Assert.Equal(KLASSE_A + " (Import 1)", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Schluessel FROM Tab_TwwDin4708Wert_STAMM WHERE ID = ?", new DbParam("@id", klassen[0]))));
            Assert.Equal(10.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Wert FROM Tab_TwwDin4708Wert_STAMM WHERE ID = ?", new DbParam("@id", klassen[0]))));

            Assert.Equal(vorher + 2, Katalogzeilen());
            Assert.Equal(2, io.LetzterBericht.Count(b => b.Contains("weicht vom namensgleichen Eintrag")
                                                          && b.Contains("(Import 1)")));

            // Zweiter Import: dieselbe mitgenommene Version, keine dritte.
            int zweit = io.Importieren(paket, "Tww Abweichend", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                       null, out fehler);
            Assert.True(zweit > 0, "Zweiter Import fehlgeschlagen: " + fehler);
            Assert.Equal(vorher + 2, Katalogzeilen());
            Assert.Equal(nutzung, Convert.ToInt64(Assert.Single(Zonen(zweit).Rows.Cast<DataRow>())["ID_Nutzungsart"]));
            Assert.Contains(io.LetzterBericht, b => b.Contains("entspricht der schon mitgenommenen Version"));
        }

        /// <summary>
        /// Nur der Tagesgangsatz weicht ab (ein Tagesgang am Ziel geändert): Der Satz kommt als
        /// neue Version samt seinen vier Tagesgängen, und die Nutzungsart — deren eigene Werte
        /// gleich sind — ebenfalls, weil sie auf den Satz des Pakets zeigen muss. Die Zone zeigt
        /// auf beide neuen Versionen.
        /// </summary>
        [Fact]
        public void Transfer_mit_abweichendem_Tagesgangsatz_nimmt_Satz_und_Nutzungsart_neu_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Stand q = Anlegen(quelle);
            string paket = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, paket));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_TwwTagesgang_STAMM SET Anteil_07 = 0.5, Anteil_08 = 0.0 WHERE ID_Tagesgangsatz = ? AND Tagtyp = 1",
                new DbParam("@id", q.Tagesgangsatz)));
            int vorher = Katalogzeilen();

            int neu = io.Importieren(paket, "Tww Satz", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string fehler);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);

            DataRow z = Assert.Single(Zonen(neu).Rows.Cast<DataRow>());
            long satz = Convert.ToInt64(z["ID_Tagesgangsatz"]);
            long nutzung = Convert.ToInt64(z["ID_Nutzungsart"]);
            Assert.NotEqual(q.Tagesgangsatz, satz);
            Assert.NotEqual(q.Nutzungsart, nutzung);
            Assert.Equal(SATZ + " (Import 1)", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Bezeichner FROM Tab_TwwTagesgangsatz_STAMM WHERE ID = ?", new DbParam("@id", satz))));
            Assert.Equal(satz, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID_Tagesgangsatz FROM Tab_TwwNutzungsart_STAMM WHERE ID = ?", new DbParam("@id", nutzung))));
            Assert.Equal(0.25, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Anteil_07 FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = ? AND Tagtyp = 1",
                new DbParam("@id", satz))));
            Assert.Equal(0.5, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Anteil_07 FROM Tab_TwwTagesgang_STAMM WHERE ID_Tagesgangsatz = ? AND Tagtyp = 1",
                new DbParam("@id", q.Tagesgangsatz))));

            // Zwei Köpfe und vier Tagesgänge neu; Bedarfstag und Ausstattung wie am Ziel.
            Assert.Equal(vorher + 2 + 4, Katalogzeilen());
            Assert.Equal(2, io.LetzterBericht.Count(b => b.Contains("weicht vom namensgleichen Eintrag")));
        }

        /// <summary>
        /// Ein präpariertes Manifest (N8): Kindtabelle mit fremder Verweisspalte, Kindtabelle mit
        /// fremdem Namen, Tww-Katalog mit anderem natürlichem Schlüssel oder Primärschlüssel. Die
        /// Bezeichner gingen sonst in SQL-Texte ein — der Import lehnt benannt ab und ändert nichts.
        /// </summary>
        [Fact]
        public void Transfer_mit_praepariertem_Manifest_wird_benannt_abgelehnt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();

            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Anlegen(quelle);
            string original = ordner.Datei("tww.wpx");
            var io = new ProjektExportImportCtrl();
            Assert.True(io.Exportieren(PROJEKT, original));
            (int Projekte, int Zonen, int Katalog) vorher = Bestand();

            var faelle = new (string Name, Action<JsonNode> Aendern, string Erwartet)[]
            {
                ("Verweisspalte", m => m["catalogChildren"][0]["parentColumn"] = "ID_Tagesgangsatz] = 0 OR [ID",
                 "unbekannte Kindtabelle"),
                ("Kindname", m => m["catalogChildren"][0]["name"] = "Tab_Projekt",
                 "unbekannte Kindtabelle"),
                ("Schluessel", m => m["catalogs"].AsArray()
                     .Single(k => (string)k["name"] == TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)["naturalKey"] =
                         new JsonArray("Bezeichner] = '' OR [Bezeichner", "Katalogversion"),
                 TwwSchema.TAB_TWW_NUTZUNGSART_STAMM),
                ("Primaerschluessel", m => m["catalogs"].AsArray()
                     .Single(k => (string)k["name"] == TwwSchema.TAB_TWW_NUTZUNGSART_STAMM)["pk"] = "Bezeichner",
                 TwwSchema.TAB_TWW_NUTZUNGSART_STAMM),
            };
            foreach (var f in faelle)
            {
                string paket = ordner.Datei("tww-" + f.Name + ".wpx");
                File.Copy(original, paket);
                ManifestAendern(paket, f.Aendern);

                int neu = io.Importieren(paket, "Tww Manifest " + f.Name, ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu <= 0, f.Name + ": Der Import haette abgelehnt werden muessen.");
                Assert.Contains(f.Erwartet, fehler);
                Assert.Contains("Import abgelehnt, nichts geändert", fehler);
                Assert.Equal(vorher, Bestand());
            }

            // Das unveränderte Paket geht weiter.
            Assert.True(io.Importieren(original, "Tww Manifest echt", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                       null, out string ok) > 0, ok);
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

        /// <summary>Projekte, Zonen und Tww-Katalogzeilen am Ziel — für „nichts geändert“.</summary>
        private static (int Projekte, int Zonen, int Katalog) Bestand() =>
            (Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Projekt")),
             Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_TwwZone")),
             Katalogzeilen());

        /// <summary>Ändert das Manifest eines Pakets an Ort und Stelle.</summary>
        private static void ManifestAendern(string paket, Action<JsonNode> aendern)
        {
            using ZipArchive zip = ZipFile.Open(paket, ZipArchiveMode.Update);
            ZipArchiveEntry m = zip.GetEntry("manifest.json");
            JsonNode manifest;
            using (var r = new StreamReader(m.Open())) manifest = JsonNode.Parse(r.ReadToEnd());
            aendern(manifest);
            m.Delete();
            using (var w = new StreamWriter(zip.CreateEntry("manifest.json").Open()))
                w.Write(manifest.ToJsonString());
        }

        /// <summary>
        /// Nimmt einen Katalog aus dem Paket: die Datei unter <c>catalogs/</c> und ihren
        /// Manifesteintrag — so sähe ein Paket aus, das nicht dieser Weg geschrieben hat.
        /// </summary>
        private static void KatalogAusPaketEntfernen(string paket, string tabelle)
        {
            using ZipArchive zip = ZipFile.Open(paket, ZipArchiveMode.Update);
            ZipArchiveEntry datei = zip.GetEntry("catalogs/" + tabelle + ".json");
            Assert.NotNull(datei);
            datei.Delete();

            ZipArchiveEntry m = zip.GetEntry("manifest.json");
            JsonNode manifest;
            using (var r = new StreamReader(m.Open())) manifest = JsonNode.Parse(r.ReadToEnd());
            JsonArray kataloge = manifest["catalogs"].AsArray();
            JsonNode eintrag = kataloge.Single(k => (string)k["name"] == tabelle);
            kataloge.Remove(eintrag);
            m.Delete();
            using (var w = new StreamWriter(zip.CreateEntry("manifest.json").Open()))
                w.Write(manifest.ToJsonString());
        }

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
