using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schreib- und Leseweg der gemessenen Reihen</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 4.8 und Kapitel 7 Zeile Z5; Schemaschritt T4 „Messreihen", Stufe Z5,
    /// Gruppe 1, Punkt 2): Einspielen, Liste, Rücklesen, Löschen, das Ersetzen einer gleichnamigen
    /// Reihe, der Rollback und die beiden Regeln der Ablage — die Reihe reist mit dem Projekt
    /// (Kopie und <c>.wpx</c>) und verschwindet mit ihm.
    ///
    /// <para>Gearbeitet wird auf einer Arbeitskopie der Testdatenbank; <b>jede Reihe ist
    /// erfunden</b> — runde Werte, ein erfundener Zähler, kein Objektdatum (Kapitel 9 K5). Projekt
    /// 1006 ist keines der fünf der CI.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class TwwMessreihenCtrlTests
    {
        /// <summary>Ein Projekt der Testdatenbank (Id 1006) — die Reihe hängt daran.</summary>
        private const string PROJEKT = "Stromspeicher mit Wärmepumpe";

        private const string NAME = "Waermemengenzaehler (erfunden)";
        private const string DATUM = "2026-09-25";

        // =============================================================================
        //  Einspielen, Liste, Ruecklesen
        // =============================================================================

        /// <summary>
        /// Ein Einspielen legt eine Zeile je Wert ab; die Liste nennt den Kopf ohne die Werte, und
        /// das Rücklesen ergibt <b>dieselbe</b> Reihe — Größe, Auflösung, Beginn, Werte und Quelle.
        /// </summary>
        [Fact]
        public void Einspielen_Liste_und_Ruecklesen_ergeben_dieselbe_Reihe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(projekt > 0);
            Assert.True(TwwMessreihenCtrl.TabelleVorhanden());
            Assert.Empty(TwwMessreihenCtrl.Liste(projekt));

            var werte = new[] { 1.5, 2.5, 0.0, 4.0 };
            TwwMessreihenimportBericht b = Einspielen(projekt, Csv(new DateTime(2025, 4, 1), 60, werte, "kWh"));
            Assert.True(b.Ok, b.Abbruch?.Klartext);
            Assert.Equal(werte.Length, b.Zeilen);
            Assert.Equal(0, b.Ersetzt);
            Assert.Empty(b.Hinweise);

            // Die Ablage: eine Zeile je Wert, der Kopf an jeder Zeile.
            Assert.Equal(4L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_TwwMessreihe WHERE ID_Projekt = ?", new DbParam("@p", projekt))));
            Assert.Equal(TwwSchema.MESSGROESSE_ENERGIE, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Groesse FROM Tab_TwwMessreihe WHERE ID_Projekt = ? AND Zeilenindex = 0", new DbParam("@p", projekt))));
            Assert.Equal("2025-04-01T00:00", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Beginn FROM Tab_TwwMessreihe WHERE ID_Projekt = ? AND Zeilenindex = 3", new DbParam("@p", projekt))));
            Assert.Equal(DATUM, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Datum_Import FROM Tab_TwwMessreihe WHERE ID_Projekt = ?", new DbParam("@p", projekt))));

            // Der Kopf der Liste - ohne die Werte zu laden.
            TwwMessreihenkopf kopf = Assert.Single(TwwMessreihenCtrl.Liste(projekt));
            Assert.Equal(NAME, kopf.Bezeichnung);
            Assert.Equal(ZapfMessgroesse.Energie, kopf.Groesse);
            Assert.Equal(60, kopf.AufloesungMin);
            Assert.Equal("2025-04-01T00:00", kopf.Beginn);
            Assert.Equal(4, kopf.Schritte);
            Assert.Equal(8.0, kopf.Menge, 12);
            Assert.Equal(4.0 / 24.0, kopf.Tage, 12);
            Assert.Equal("Probe (erfunden)", kopf.Quelle);
            Assert.Equal(DATUM, kopf.DatumImport);

            // Zurueckgelesen: dieselbe Reihe.
            Messreihe r = TwwMessreihenCtrl.Lesen(projekt, NAME, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(NAME, r.Bezeichnung);
            Assert.Equal(ZapfMessgroesse.Energie, r.Groesse);
            Assert.Equal(60, r.AufloesungMin);
            Assert.Equal(new DateTime(2025, 4, 1), r.Beginn);
            Assert.Equal(werte, r.Werte);
            Assert.Equal(8.0, r.Menge, 12);
            Assert.Equal("Probe (erfunden)", r.Quelle);
        }

        /// <summary>
        /// Die Menge einer Leistungs- und einer Volumenreihe steht in der Liste in der Einheit der
        /// Bilanz: kW werden über die Schrittlänge zu kWh, m³ bleiben m³. Zwei Reihen desselben
        /// Projekts stehen nebeneinander, nach Bezeichnung geordnet.
        /// </summary>
        [Fact]
        public void Die_Liste_rechnet_Leistung_in_Energie_und_ordnet_nach_Bezeichnung()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);

            // 2 kW in Viertelstunden = 0,5 kWh je Schritt.
            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 15, new[] { 2.0, 2.0 }, "kW"),
                                   "Zaehler B (erfunden)").Ok);
            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 60, new[] { 0.1, 0.3 }, "m³"),
                                   "Zaehler A (erfunden)").Ok);

            IReadOnlyList<TwwMessreihenkopf> liste = TwwMessreihenCtrl.Liste(projekt);
            Assert.Equal(2, liste.Count);
            Assert.Equal(new[] { "Zaehler A (erfunden)", "Zaehler B (erfunden)" }, liste.Select(k => k.Bezeichnung));

            Assert.Equal(ZapfMessgroesse.Volumen, liste[0].Groesse);
            Assert.Equal(0.4, liste[0].Menge, 12);
            Assert.Equal(ZapfMessgroesse.Leistung, liste[1].Groesse);
            Assert.Equal(1.0, liste[1].Menge, 12);
        }

        /// <summary>
        /// <b>Eine Reihe je Bezeichnung und Projekt:</b> Ein zweites Einspielen unter demselben
        /// Namen ERSETZT die erste vollständig — auch wenn die neue kürzer ist — und nennt das
        /// benannt. Ein anderer Name steht daneben.
        /// </summary>
        [Fact]
        public void Ein_zweites_Einspielen_ersetzt_die_gleichnamige_Reihe()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);

            Assert.Equal(4, Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 60, new[] { 1.0, 1.0, 1.0, 1.0 }, "kWh")).Zeilen);
            TwwMessreihenimportBericht zweit =
                Einspielen(projekt, Csv(new DateTime(2025, 2, 1), 15, new[] { 3.0, 3.0 }, "kWh"));
            Assert.True(zweit.Ok);
            Assert.Equal(2, zweit.Zeilen);
            Assert.Equal(4, zweit.Ersetzt);
            Assert.Contains(zweit.Hinweise, h => h.Kennung == "MESSREIHENIMPORT_ERSETZT");

            Assert.Equal(2L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_TwwMessreihe WHERE ID_Projekt = ?", new DbParam("@p", projekt))));
            Messreihe r = TwwMessreihenCtrl.Lesen(projekt, NAME, out _);
            Assert.Equal(15, r.AufloesungMin);
            Assert.Equal(new DateTime(2025, 2, 1), r.Beginn);
        }

        /// <summary>
        /// <b>Der Rollback</b> (Muster <c>TwwTyptagCtrl</c>): Scheitert der Vorgang mitten im
        /// Schreiben, steht die frühere Reihe unverändert da — kein halber Stand. Die Prüfnaht
        /// läuft INNERHALB der Transaktion, nachdem die alten Zeilen weg und die erste neue
        /// geschrieben ist.
        /// </summary>
        [Fact]
        public void Ein_Fehler_mitten_im_Schreiben_rollt_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);

            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 60, new[] { 1.0, 2.0, 3.0 }, "kWh")).Ok);

            TwwMessreihenCtrl.Pruefnaht = () => throw new InvalidOperationException("Probe des Rollbacks");
            try
            {
                TwwMessreihenimportBericht b =
                    Einspielen(projekt, Csv(new DateTime(2025, 6, 1), 15, new[] { 9.0, 9.0 }, "kWh"));
                Assert.False(b.Ok);
                Assert.Equal("MESSREIHENIMPORT_FEHLGESCHLAGEN", b.Abbruch.Kennung);
                Assert.Equal(0, b.Zeilen);
                Assert.Contains("Probe des Rollbacks", b.Abbruch.Klartext);
            }
            finally
            {
                TwwMessreihenCtrl.Pruefnaht = () => { };
            }

            // Die fruehere Reihe steht unveraendert.
            Messreihe r = TwwMessreihenCtrl.Lesen(projekt, NAME, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(60, r.AufloesungMin);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, r.Werte);
        }

        /// <summary>
        /// Jede Ablehnung des Schreibwegs ist benannt: eine Datei, die der Leser ablehnt, ein
        /// Projekt, das es nicht gibt, eine Reihe, die das Projekt nicht führt, und keine Reihe.
        /// </summary>
        [Fact]
        public void Jede_Ablehnung_des_Schreibwegs_ist_benannt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);

            // Eine unbrauchbare Datei: der Grund kommt vom LESER durch.
            TwwMessreihenimportBericht kaputt = Einspielen(projekt, "Zeitstempel;Wert [kWh]\n2025-01-01T00:00;viel\n");
            Assert.False(kaputt.Ok);
            Assert.Equal("MESSREIHE_KEINE_ZAHL", kaputt.Abbruch.Kennung);
            Assert.Equal(0, kaputt.Zeilen);

            // Ein Projekt, das es nicht gibt.
            TwwMessreihenimportBericht fremd = Einspielen(987654, Csv(new DateTime(2025, 1, 1), 60, new[] { 1.0, 1.0 }, "kWh"));
            Assert.False(fremd.Ok);
            Assert.Equal("MESSREIHENIMPORT_FEHLGESCHLAGEN", fremd.Abbruch.Kennung);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_TwwMessreihe WHERE ID_Projekt = ?", new DbParam("@p", 987654))));

            // Keine Reihe.
            Assert.Equal("MESSREIHENIMPORT_OHNE_REIHE",
                         TwwMessreihenCtrl.Importieren(projekt, (Messreihe)null).Abbruch.Kennung);

            // Eine Reihe, die das Projekt nicht fuehrt.
            Assert.Null(TwwMessreihenCtrl.Lesen(projekt, "gibt es nicht", out ZapfSatz fehlt));
            Assert.Equal("MESSREIHENIMPORT_NICHT_GEFUNDEN", fehlt.Kennung);

            // Eine Luecke im Zeilenindex waere eine zeitlich verschobene Reihe - benannt.
            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 60, new[] { 1.0, 2.0, 3.0 }, "kWh")).Ok);
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_TwwMessreihe WHERE ID_Projekt = ? AND Zeilenindex = 1",
                                           new DbParam("@p", projekt));
            Assert.Null(TwwMessreihenCtrl.Lesen(projekt, NAME, out ZapfSatz luecke));
            Assert.Equal("MESSREIHENIMPORT_ZEILENINDEX_LUECKE", luecke.Kennung);
        }

        /// <summary>
        /// Löschen entfernt genau eine Reihe; ein zweites Löschen ist folgenlos (0 Zeilen), und die
        /// zweite Reihe des Projekts bleibt.
        /// </summary>
        [Fact]
        public void Loeschen_trifft_eine_Reihe_und_ist_wiederholbar()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);

            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 60, new[] { 1.0, 1.0 }, "kWh")).Ok);
            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 60, new[] { 2.0, 2.0, 2.0 }, "kWh"),
                                   "Zweiter Zaehler (erfunden)").Ok);

            Assert.Equal(2, TwwMessreihenCtrl.Loeschen(projekt, NAME));
            Assert.Equal(0, TwwMessreihenCtrl.Loeschen(projekt, NAME));
            TwwMessreihenkopf rest = Assert.Single(TwwMessreihenCtrl.Liste(projekt));
            Assert.Equal("Zweiter Zaehler (erfunden)", rest.Bezeichnung);
        }

        // =============================================================================
        //  Die zwei Regeln der Ablage
        // =============================================================================

        /// <summary>
        /// <b>Die Reihe gehört dem Projekt:</b> Eine Projektkopie nimmt sie mit (über
        /// <c>ID_Projekt</c>, ohne einen eigenen Eintrag im Kopierplan), und das Löschen des
        /// Projekts räumt sie ab (<c>ON DELETE CASCADE</c>). Die Kopie ist unabhängig.
        /// </summary>
        [Fact]
        public void Eine_Projektkopie_nimmt_die_Messreihen_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            var dup = new ProjektDuplizierenCtrl();
            int quelle = dup.GetProjektId(PROJEKT);
            Assert.True(Einspielen(quelle, Csv(new DateTime(2025, 5, 1), 60, new[] { 1.0, 2.0, 3.0 }, "kWh")).Ok);

            int neu = dup.Duplizieren(PROJEKT, "Messreihe Kopie");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            Messreihe kopie = TwwMessreihenCtrl.Lesen(neu, NAME, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(new[] { 1.0, 2.0, 3.0 }, kopie.Werte);
            Assert.Equal(new DateTime(2025, 5, 1), kopie.Beginn);

            // Unabhaengig: Die Quellreihe zu loeschen laesst die Kopie stehen.
            Assert.Equal(3, TwwMessreihenCtrl.Loeschen(quelle, NAME));
            Assert.Equal(3, TwwMessreihenCtrl.Liste(neu).Single().Schritte);

            // Und das Projekt raeumt seine Reihen mit sich ab (ON DELETE CASCADE).
            DataRepository.ExecuteNonQuery("PRAGMA foreign_keys = ON");
            Assert.True(DataRepository.ExecuteSQL("DELETE FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", neu)));
            Assert.Empty(TwwMessreihenCtrl.Liste(neu));
        }

        /// <summary>
        /// <b>Der Transfer trägt die Messreihen mit</b> (<c>.wpx</c>): Sie sind Bestandteil des
        /// Projekts (Kapitel 9 K5), tragen <c>ID_Projekt</c> und stehen deshalb im Transferplan;
        /// nach der Rundreise steht dieselbe Reihe am Ziel.
        /// </summary>
        [Fact]
        public void Der_Transfer_traegt_die_Messreihen_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            using var ordner = new Arbeitsordner();
            int quelle = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(Einspielen(quelle, Csv(new DateTime(2025, 7, 1), 15, new[] { 0.5, 1.5, 2.5, 0.0 }, "kWh")).Ok);

            var io = new ProjektExportImportCtrl();
            Assert.Contains(TwwSchema.TAB_TWW_MESSREIHE, io.Transferplan().Select(s => s.Tabelle));

            string paket = ordner.Datei("messreihe.wpx");
            Assert.True(io.Exportieren(PROJEKT, paket));
            int neu = io.Importieren(paket, "Messreihe Transfer", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                     null, out string grund);
            Assert.True(neu > 0, "Import fehlgeschlagen: " + grund);

            Messheimkehr(neu);
        }

        private static void Messheimkehr(int projekt)
        {
            Messreihe r = TwwMessreihenCtrl.Lesen(projekt, NAME, out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.Equal(15, r.AufloesungMin);
            Assert.Equal(new DateTime(2025, 7, 1), r.Beginn);
            Assert.Equal(new[] { 0.5, 1.5, 2.5, 0.0 }, r.Werte);
            Assert.Equal(ZapfMessgroesse.Energie, r.Groesse);
        }

        /// <summary>
        /// <b>Die Auslieferungsvorlage leert die Tabelle</b> — auch für ein Beispielprojekt
        /// (Kapitel 9 K5). Die Probe hält den Quelltext des Werkzeugs dagegen: Es löscht die
        /// Tabelle und prüft sie als eigenen Posten; die Tabelle steht in keiner Liste, die
        /// Zeilen stehen lässt.
        /// </summary>
        [Fact]
        public void Die_Auslieferungsvorlage_leert_die_Messreihen()
        {
            string datei = Werkzeugquelle();
            if (datei == null) return;
            string text = File.ReadAllText(datei);
            Assert.Contains("internal const string TAB_MESSREIHE = TwwSchema.TAB_TWW_MESSREIHE;", text, StringComparison.Ordinal);
            Assert.Contains("\"DELETE FROM \\\"\" + TAB_MESSREIHE + \"\\\"\"", text, StringComparison.Ordinal);
            Assert.Contains("messdaten", text, StringComparison.Ordinal);
        }

        // =============================================================================
        //  Nulllaeufe und Aufwand
        // =============================================================================

        /// <summary>
        /// <b>Die Zahl der Nullläufe geht an den Vergleich</b> (Befund 7): Sie steht nicht als
        /// Kopfwert in der Tabelle, wird beim Rücklesen aber gezählt und der Reihe übergeben — sonst
        /// stünde eine zurückgelesene Reihe mit <c>Luecken = 0</c> da und Vergleich und Kalibrierung
        /// hielten eine halb leere Messung für vollständig. Ein benannter Hinweis sagt dazu, dass die
        /// Ablage gefüllte Lücken und Zeiten ohne Zapfung nicht unterscheidet.
        /// </summary>
        [Fact]
        public void Das_Ruecklesen_zaehlt_die_Nulllaeufe_und_nennt_sie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(projekt > 0);

            // Acht Stunden, drei davon ohne Wert (die CSV laesst sie leer -> der Leser fuellt 0).
            var werte = new[] { 1.0, 0.0, 2.0, 0.0, 3.0, 0.0, 4.0, 5.0 };
            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 4, 1), 60, werte, "kWh")).Ok);

            var hinweise = new List<ZapfSatz>();
            Messreihe r = TwwMessreihenCtrl.Lesen(projekt, NAME, out ZapfSatz fehler, hinweise);
            Assert.Null(fehler);
            Assert.Equal(werte, r.Werte);
            Assert.Equal(3, r.Luecken);
            Assert.Equal(3.0 / 8.0, r.Lueckenanteil, 12);
            ZapfSatz hinweis = Assert.Single(hinweise, h => h.Kennung == "MESSREIHENIMPORT_NULLLAEUFE");
            Assert.NotEmpty(hinweis.Klartext);

            // Ohne Nulllauf steht kein Hinweis - und die Luecken sind 0.
            Assert.True(TwwMessreihenCtrl.Loeschen(projekt, NAME) > 0);
            Assert.True(Einspielen(projekt, Csv(new DateTime(2025, 4, 1), 60, new[] { 1.0, 2.0, 3.0 }, "kWh")).Ok);
            var ohne = new List<ZapfSatz>();
            Messreihe voll = TwwMessreihenCtrl.Lesen(projekt, NAME, out _, ohne);
            Assert.Equal(0, voll.Luecken);
            Assert.DoesNotContain(ohne, h => h.Kennung == "MESSREIHENIMPORT_NULLLAEUFE");

            // Und ein Schalttag der zurueckgelesenen Reihe wird ebenso benannt wie beim Einlesen.
            Assert.True(TwwMessreihenCtrl.Loeschen(projekt, NAME) > 0);
            var tage = Enumerable.Repeat(1.0, 3 * 24).ToArray();          // 28.02. bis 01.03.2024
            Assert.True(Einspielen(projekt, Csv(new DateTime(2024, 2, 28), 60, tage, "kWh")).Ok);
            var schalt = new List<ZapfSatz>();
            Messreihe ueberSchalttag = TwwMessreihenCtrl.Lesen(projekt, NAME, out _, schalt);
            Assert.Equal(1, ueberSchalttag.Schalttage);
            Assert.Single(schalt, h => h.Kennung == "MESSREIHE_SCHALTTAG");
        }

        /// <summary>
        /// <b>Der Aufwand</b> (Befund 8): <b>100 000 Zeilen einspielen und zurücklesen dauert unter
        /// fünf Sekunden.</b> Das Einspielen läuft über EIN vorbereitetes Kommando, dessen Parameter
        /// je Zeile neu belegt werden, das Rücklesen über einen Reader ohne <c>DataTable</c> — beides
        /// zusammen macht den Unterschied zwischen einer Sekunde und einer Minute.
        ///
        /// <para><b>Die Schranke ist grob mit Absicht:</b> Sie soll eine Größenordnung fangen (ein
        /// Kommando je Zeile, eine <c>DataTable</c> mit sechs Spalten), nicht eine Zehntelsekunde
        /// messen — ein Bauserver ist langsamer als eine Arbeitsstation, und eine scharfe Schranke
        /// wäre dort rot, ohne dass sich etwas verschlechtert hätte.</para>
        /// </summary>
        [Fact]
        public void Hunderttausend_Zeilen_brauchen_unter_fuenf_Sekunden()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            int projekt = new ProjektDuplizierenCtrl().GetProjektId(PROJEKT);
            Assert.True(projekt > 0);

            // 100 000 Minutenwerte, rund und erfunden: ein Sägezahn 0,00 bis 0,99 l/min gibt es
            // nicht - hier kWh je Minute, damit keine Spreizung nötig ist.
            const int zeilen = 100000;
            var werte = new double[zeilen];
            for (int i = 0; i < zeilen; i++) werte[i] = (i % 100) / 100.0;

            var uhr = System.Diagnostics.Stopwatch.StartNew();
            TwwMessreihenimportBericht b = Einspielen(projekt, Csv(new DateTime(2025, 1, 1), 1, werte, "kWh"));
            Assert.True(b.Ok, b.Abbruch?.Klartext);
            Assert.Equal(zeilen, b.Zeilen);

            Messreihe r = TwwMessreihenCtrl.Lesen(projekt, NAME, out ZapfSatz fehler);
            uhr.Stop();
            Assert.Null(fehler);
            Assert.Equal(zeilen, r.Schritte);
            Assert.Equal(werte[12345], r.Werte[12345], 12);
            Assert.Equal(werte.Sum(), r.Menge, 6);
            Assert.True(uhr.Elapsed.TotalSeconds < 5.0,
                        "Einspielen und Ruecklesen von " + zeilen + " Zeilen dauerten " +
                        uhr.Elapsed.TotalSeconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) +
                        " s (Schranke 5 s).");
        }

        // =============================================================================
        //  Helfer
        // =============================================================================

        private static TwwMessreihenimportBericht Einspielen(int projekt, string csv, string bezeichnung = NAME)
        {
            var optionen = new Messreihenoptionen { Bezeichnung = bezeichnung, Quelle = "Probe (erfunden)" };
            byte[] roh = new UTF8Encoding(true).GetBytes(csv);
            using var strom = new MemoryStream(roh);
            return TwwMessreihenCtrl.Importieren(projekt, strom, "messung.csv", optionen, DATUM);
        }

        /// <summary>Eine CSV im ISO-Format mit der Einheit im Kopf der Wertspalte.</summary>
        private static string Csv(DateTime beginn, int rasterMin, IReadOnlyList<double> werte, string einheit)
        {
            var bau = new StringBuilder("Zeitstempel;Wert [" + einheit + "]\n");
            for (int i = 0; i < werte.Count; i++)
                bau.Append(beginn.AddMinutes((double)i * rasterMin).ToString("yyyy-MM-ddTHH:mm",
                                                                            System.Globalization.CultureInfo.InvariantCulture))
                   .Append(';').Append(werte[i].ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture))
                   .Append('\n');
            return bau.ToString();
        }

        /// <summary>Ein Ordner für das Paket der Rundreise; er verschwindet mit dem Testfall.</summary>
        private sealed class Arbeitsordner : IDisposable
        {
            private readonly string _pfad;

            public Arbeitsordner()
            {
                _pfad = Path.Combine(Path.GetTempPath(),
                                     "epos-messreihe-" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(_pfad);
            }

            public string Datei(string name) => Path.Combine(_pfad, name);

            public void Dispose()
            {
                try { Directory.Delete(_pfad, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary><c>Werkzeuge/Auslieferungsvorlage/TwwKataloge.cs</c>, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Werkzeugquelle()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(d.FullName, "Werkzeuge", "Auslieferungsvorlage", "TwwKataloge.cs");
                if (File.Exists(kandidat)) return kandidat;
            }
            return null;
        }
    }
}
