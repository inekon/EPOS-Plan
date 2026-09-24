using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Schreibweg der eingespielten Typtage</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 3.1, 3.3; Schemaschritt T3 „Typtage", Stufe Z4b) auf einer ARBEITSKOPIE der
    /// Testdatenbank: Einspielen, Stand, Zurücklesen, ein zweites Paket ersetzt das erste,
    /// Löschen, Rollback — und die Tabelle wandert in keine Projektkopie.
    ///
    /// <para><b>Die Testdatenbank bleibt ohne Typtage.</b> Jede Zeile dieser Klasse entsteht in
    /// der Kopie aus einem ERFUNDENEN Paket (<see cref="Typtagpaketbauer"/>); das Repositorium
    /// trägt keinen Wert einer Richtlinie (Konzept Kapitel 6).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwTyptagCtrlTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            TwwTyptagCtrl.Pruefnaht = () => { };
            _kultur.Dispose();
        }

        private const int ZONE = 3;
        private const string ART = "probehaus";

        // =================================================================================
        //  Stand und Einspielen
        // =================================================================================

        [Fact]
        public void Ohne_eingespielte_Typtage_meldet_der_Stand_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(TwwTyptagCtrl.TabelleVorhanden(), "Die Kopie steht nicht auf Schritt 131.");
            TwwTyptagstand stand = TwwTyptagCtrl.Stand();
            Assert.False(stand.Vorhanden);
            Assert.Equal(0, stand.Zeilen);
            Assert.Empty(stand.Klimazonen);
            Assert.Null(TwwTyptagCtrl.Lesen());
        }

        [Fact]
        public void Ein_Paket_wird_eingespielt_und_kommt_gleich_zurueck()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Typtagpaketbauer b = Typtagpaketbauer.Erfunden(ZONE, ART);
            foreach (var k in b.Kategorien) b.Gaenge.Add((ART, k.Code, 720, new[] { 0.25, 0.75 }));
            TwwTyptagimportBericht bericht = TwwTyptagCtrl.Importieren(b.Dateien(), "2026-09-24");

            Assert.Null(bericht.Abbruch);
            Assert.True(bericht.Zeilen > 0);
            Assert.Equal(0, bericht.Ersetzt);

            // Der Stand nennt Zonen, Gebäudearten, Typtage, Quelle und Tag des Einspielens.
            TwwTyptagstand stand = bericht.Stand;
            Assert.True(stand.Vorhanden);
            Assert.Equal(new[] { ZONE }, stand.Klimazonen);
            Assert.Equal(new[] { ART }, stand.Gebaeudearten);
            Assert.Equal(6, stand.Typtage.Count);
            Assert.True(stand.MitTagesgaengen);
            Assert.Equal("Anwenderpaket (erfunden, Probe)", stand.Quelle);
            Assert.Equal("2026-09", stand.Ausgabe);
            Assert.Equal("2026-09-24", stand.DatumImport);

            // Zurückgelesen ist der Satz derselbe wie der gelesene.
            Normformvektorsatz gelesen = TwwTyptagCtrl.Lesen();
            Normformvektorsatz erwartet = Normformvektorleser.AusDateien(b.Dateien(), out ZapfSatz fehler);
            Assert.Null(fehler);
            Assert.NotNull(gelesen);
            Assert.Equal(erwartet.Kategorien.Select(k => k.Code).OrderBy(x => x, StringComparer.Ordinal),
                         gelesen.Kategorien.Select(k => k.Code).OrderBy(x => x, StringComparer.Ordinal));
            foreach (Typtagkategorie k in erwartet.Kategorien)
            {
                Typtagkategorie g = gelesen.Kategorie(k.Code);
                Assert.NotNull(g);
                Assert.Equal(k.Jahreszeit, g.Jahreszeit);
                Assert.Equal(k.Tagart, g.Tagart);
                Assert.Equal(k.Bewoelkung, g.Bewoelkung);
                Assert.Equal(erwartet.Anzahl(ZONE, ART, k.Code), gelesen.Anzahl(ZONE, ART, k.Code));
                Assert.Equal(erwartet.Faktor(ZONE, ART, k.Code).Value, gelesen.Faktor(ZONE, ART, k.Code).Value, 12);
                Assert.Equal(erwartet.Gang(ART, k.Code).Anteile, gelesen.Gang(ART, k.Code).Anteile);
            }
            Assert.Equal(erwartet.Kennwerte.Count, gelesen.Kennwerte.Count);
            foreach (KeyValuePair<string, double> kw in erwartet.Kennwerte)
                Assert.Equal(kw.Value, gelesen.Kennwert(kw.Key).Value, 12);
            Assert.Equal(365, gelesen.Tagesumme(ZONE, ART));
        }

        [Fact]
        public void Ein_Paket_aus_einem_Strom_geht_denselben_Weg()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Typtagpaketbauer b = Typtagpaketbauer.MitBewoelkung(ZONE, ART);
            string ordner = Path.Combine(Path.GetTempPath(), "epos-typtagstrom-" + Guid.NewGuid().ToString("N"));
            try
            {
                string zip = b.AlsZip(ordner);
                using (FileStream fs = File.OpenRead(zip))
                {
                    TwwTyptagimportBericht bericht = TwwTyptagCtrl.Importieren(fs, "2026-09-24");
                    Assert.Null(bericht.Abbruch);
                    Assert.True(bericht.Stand.Vorhanden);
                    Assert.Equal(10, bericht.Stand.Typtage.Count);
                }
                Assert.Equal(Typtagpaketbauer.GRENZE_BEWOELKUNG, TwwTyptagCtrl.Lesen().Kennwert(Typtagkennwert.BEWOELKUNG_SCHWELLE));
            }
            finally
            {
                if (Directory.Exists(ordner)) Directory.Delete(ordner, true);
            }
        }

        [Fact]
        public void Das_zweite_Paket_ersetzt_das_erste()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            TwwTyptagimportBericht erst = TwwTyptagCtrl.Importieren(Typtagpaketbauer.Erfunden(ZONE, ART).Dateien());
            Assert.Null(erst.Abbruch);
            int zeilenErst = erst.Zeilen;

            // Ein anderes Paket: andere Zone, andere Gebäudeart, mehr Kategorien.
            TwwTyptagimportBericht zweit = TwwTyptagCtrl.Importieren(
                Typtagpaketbauer.MitBewoelkung(7, "anderhaus").Dateien());
            Assert.Null(zweit.Abbruch);
            Assert.Equal(zeilenErst, zweit.Ersetzt);
            Assert.Equal(zweit.Zeilen, zweit.Stand.Zeilen);
            Assert.Equal(new[] { 7 }, zweit.Stand.Klimazonen);
            Assert.Equal(new[] { "anderhaus" }, zweit.Stand.Gebaeudearten);

            Normformvektorsatz gelesen = TwwTyptagCtrl.Lesen();
            Assert.Equal(10, gelesen.Kategorien.Count);
            Assert.DoesNotContain(ZONE, gelesen.Klimazonen);
            Assert.DoesNotContain(ART, gelesen.Gebaeudearten);
        }

        [Fact]
        public void Loeschen_raeumt_die_Typtage_fort()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            TwwTyptagimportBericht bericht = TwwTyptagCtrl.Importieren(Typtagpaketbauer.Erfunden(ZONE, ART).Dateien());
            Assert.Null(bericht.Abbruch);

            Assert.Equal(bericht.Zeilen, TwwTyptagCtrl.Loeschen());
            Assert.False(TwwTyptagCtrl.Stand().Vorhanden);
            Assert.Null(TwwTyptagCtrl.Lesen());
            Assert.Equal(0, TwwTyptagCtrl.Loeschen());
        }

        // =================================================================================
        //  Rollback
        // =================================================================================

        [Fact]
        public void Ein_Fehler_im_Schreibvorgang_laesst_den_frueheren_Stand_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            TwwTyptagimportBericht erst = TwwTyptagCtrl.Importieren(Typtagpaketbauer.Erfunden(ZONE, ART).Dateien());
            Assert.Null(erst.Abbruch);
            TwwTyptagstand vorher = TwwTyptagCtrl.Stand();

            TwwTyptagCtrl.Pruefnaht = () => throw new InvalidOperationException("Probe des Rollbacks");
            try
            {
                TwwTyptagimportBericht zweit = TwwTyptagCtrl.Importieren(
                    Typtagpaketbauer.MitBewoelkung(7, "anderhaus").Dateien());
                Assert.NotNull(zweit.Abbruch);
                Assert.Equal("TYPTAGIMPORT_FEHLGESCHLAGEN", zweit.Abbruch.Kennung);
                Assert.Equal(0, zweit.Zeilen);
            }
            finally
            {
                TwwTyptagCtrl.Pruefnaht = () => { };
            }

            // Der frühere Satz steht unverändert da: kein halber Stand.
            TwwTyptagstand nachher = TwwTyptagCtrl.Stand();
            Assert.Equal(vorher.Zeilen, nachher.Zeilen);
            Assert.Equal(vorher.Klimazonen, nachher.Klimazonen);
            Assert.Equal(vorher.Gebaeudearten, nachher.Gebaeudearten);
            Assert.Equal(6, TwwTyptagCtrl.Lesen().Kategorien.Count);
        }

        [Fact]
        public void Ein_untaugliches_Paket_aendert_nichts()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            TwwTyptagCtrl.Importieren(Typtagpaketbauer.Erfunden(ZONE, ART).Dateien());
            TwwTyptagstand vorher = TwwTyptagCtrl.Stand();

            List<TwwPaketdatei> kaputt = Typtagpaketbauer.Ohne(
                Typtagpaketbauer.MitBewoelkung(7, "anderhaus").Dateien(), Normformvektorleser.DATEI_FAKTOREN);
            TwwTyptagimportBericht bericht = TwwTyptagCtrl.Importieren(kaputt);

            Assert.Equal("NORMVEKTOR_DATEI_FEHLT", bericht.Abbruch.Kennung);
            Assert.Equal(0, bericht.Zeilen);
            Assert.Equal(vorher.Zeilen, TwwTyptagCtrl.Stand().Zeilen);
        }

        // =================================================================================
        //  Die Typtage sind anwenderlokal
        // =================================================================================

        /// <summary>
        /// Die eingespielten Typtage wandern in keine Projektkopie: Die Tabelle steht nicht im
        /// Plan des <c>ProjektDuplizierenCtrl</c> — sie führt kein <c>ID_Projekt</c> und hängt an
        /// keiner Elternzeile eines Projekts (Konzept 3.1, Kapitel 6).
        /// </summary>
        [Fact]
        public void Die_Typtage_stehen_in_keinem_Projekttransfer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            TwwTyptagCtrl.Importieren(Typtagpaketbauer.Erfunden(ZONE, ART).Dateien());

            var dup = new ProjektDuplizierenCtrl();
            List<ProjektDuplizierenCtrl.Spec> plan = dup.ErmittlePlan();
            Assert.DoesNotContain(plan, s => string.Equals(s.Tabelle, TwwSchema.TAB_TWW_TYPTAG_IMPORT,
                                                           StringComparison.OrdinalIgnoreCase));
            // Die Tww-Projekttabellen stehen dagegen drin - der Plan ist also der richtige.
            Assert.Contains(plan, s => string.Equals(s.Tabelle, TwwSchema.TAB_TWW_ZONE, StringComparison.OrdinalIgnoreCase));

            // Das .wpx-Paket nimmt nur Katalogtabellen mit; die Typtage tragen kein "_STAMM".
            Assert.DoesNotContain("_STAMM", TwwSchema.TAB_TWW_TYPTAG_IMPORT);
        }
    }
}
