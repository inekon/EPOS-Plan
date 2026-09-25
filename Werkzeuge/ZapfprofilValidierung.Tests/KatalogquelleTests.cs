using System;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Die beiden Katalogquellen</b>: der Paketordner im Format N2 (Kapitel 6 (b)) und eine
    /// <c>.sqlite</c>-Datei. Beide enden im gleichen Bau; geprüft wird, dass daraus wirklich die
    /// Modelle des Kerns entstehen — datenbankfrei und ohne Controller.
    ///
    /// <para>Der SQLite-Fall läuft gegen eine <b>Kopie</b> von
    /// <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>; fehlt die Datei, schweigt er (Muster
    /// <c>EPOS.Kern.Tests/TestDatenbank</c>). Die Quelle wird ohnehin nur unveränderlich geöffnet,
    /// die Kopie ist der zweite Riegel.</para>
    /// </summary>
    public sealed class KatalogquelleTests
    {
        [Fact]
        public void Der_Beispielpaketordner_wird_zu_Modellen_des_Kerns()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;

            Katalog k = Katalogquelle.Lesen(v.Katalog, out string fehler);
            Assert.Null(fehler);
            Assert.NotNull(k);
            Assert.Equal(2, k.Arten.Count);
            Assert.Equal(2, k.Saetze.Count);
            Assert.Equal(8, k.Kategorien.Count);
            Assert.Empty(k.Hinweise);

            Nutzungsart wohnen = k.Suchen("Beispiel Wohnen (erfunden)");
            Assert.NotNull(wohnen);
            Assert.Equal(ZapfBezugsart.Personen, wohnen.Bezug);
            Assert.Equal(ZapfBilanzgrenze.MitVerteilung, wohnen.Grenze);
            Assert.Equal(ZapfKalenderart.Wohnen, wohnen.Kalender);
            Assert.Equal(60.0, wohnen.Bezugstemperaturen.ZapftemperaturC);
            Assert.Equal(12.0, wohnen.Bezugstemperaturen.KaltwasserC);
            Assert.Equal(3, wohnen.BedarfJeNiveauKwhJeEinheitTag.Length);
            Assert.Equal(12, wohnen.Monatsfaktoren.Length);
            Assert.Equal(7, wohnen.Wochenfaktoren.Length);
            Assert.Equal(1.0, wohnen.Wochenfaktoren.Sum(), 9);
            Assert.Null(wohnen.Ferienfaktor);
            Assert.True(wohnen.ReadOnly);
            Assert.Equal(ZapfKatalogstatus.Auslieferung, wohnen.Status);

            // Der Tagesgangsatz traegt vier Tagtypen, je Summe 1 - sonst waere der Formvektor falsch.
            Assert.True(wohnen.Tagesgaenge.Vollstaendig);
            for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
            {
                double summe = 0.0;
                for (int h = 0; h < Tagesgangsatz.STUNDEN; h++) summe += wohnen.Tagesgaenge.Anteile[t, h];
                Assert.Equal(1.0, summe, 9);
            }

            Nutzungsart pflege = k.Suchen("Beispiel Pflegeheim (erfunden)");
            Assert.NotNull(pflege);
            Assert.Equal(ZapfBezugsart.Betten, pflege.Bezug);
            Assert.Equal(ZapfKalenderart.Arbeitstage, pflege.Kalender);
            Assert.Equal(0.5, pflege.Ferienfaktor);

            // Der Parametersatz traegt jeden Schluessel, den Bilanz, Validierung und Stochastik brauchen.
            foreach (string s in new[] { ZapfParameter.KALTWASSER_MITTEL, ZapfParameter.KALTWASSER_AMPLITUDE,
                                         ZapfParameter.KALTWASSER_MONAT_MAXIMUM, ZapfParameter.ZIRKULATION_ANTEIL,
                                         ZapfParameter.ZIRKULATION_LAUFZEIT, ZapfParameter.VALIDIERUNG_BAND_UNTEN,
                                         ZapfParameter.VALIDIERUNG_BAND_OBEN, ZapfParameter.VALIDIERUNG_FORMSCHWELLE,
                                         ZapfParameter.VALIDIERUNG_LUECKENANTEIL,
                                         ZapfParameter.VALIDIERUNG_KALIBRIERUNG_TAGE,
                                         ZapfStochastikParameter.URLAUBSVERSATZ,
                                         ZapfStochastikParameter.KONSISTENZSCHWELLE })
                Assert.True(k.Parameter.Enthaelt(s), "Dem Beispielkatalog fehlt der Parameter " + s + ".");
            Assert.Equal(0.85, k.Parameter.Wert(ZapfParameter.VALIDIERUNG_BAND_UNTEN));
            Assert.Equal(0.95, k.Parameter.Wert(ZapfParameter.VALIDIERUNG_BAND_OBEN));

            // Die Kategorien haengen an den Nutzungsarten und tragen Anteile in Summe 1.
            foreach (Nutzungsart a in k.Arten)
            {
                var eigene = k.Kategorien.Where(z => z.IdNutzungsart == a.Id).ToList();
                Assert.Equal(4, eigene.Count);
                Assert.Equal(1.0, eigene.Sum(z => z.Anteil), 9);
            }
        }

        [Fact]
        public void Eine_fehlende_Pflichtdatei_im_Paketordner_wird_benannt()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string ordner = v.Neu("halbespaket");
            foreach (string d in Directory.EnumerateFiles(v.Katalog))
                if (!Path.GetFileName(d).Equals(Katalogquelle.DATEI_PARAMETER, StringComparison.Ordinal))
                    File.Copy(d, Path.Combine(ordner, Path.GetFileName(d)));

            Assert.Null(Katalogquelle.Lesen(ordner, out string fehler));
            Assert.Contains(Katalogquelle.DATEI_PARAMETER, fehler, StringComparison.Ordinal);
        }

        [Fact]
        public void Ohne_Kategoriendatei_wird_der_deterministische_Weg_benannt()
        {
            using var v = new Vorrichtung();
            if (!v.BeispielDa) return;
            string ordner = v.Neu("ohnekategorien");
            foreach (string d in Directory.EnumerateFiles(v.Katalog))
                if (!Path.GetFileName(d).Equals(Katalogquelle.DATEI_ZAPFKATEGORIE, StringComparison.Ordinal))
                    File.Copy(d, Path.Combine(ordner, Path.GetFileName(d)));

            Katalog k = Katalogquelle.Lesen(ordner, out string fehler);
            Assert.Null(fehler);
            Assert.Empty(k.Kategorien);
            Assert.Contains(k.Hinweise, h => h.Contains("deterministische", StringComparison.Ordinal));
        }

        [Fact]
        public void Eine_Quelle_die_es_nicht_gibt_wird_benannt()
        {
            Assert.Null(Katalogquelle.Lesen("gibt-es-nicht", out string a));
            Assert.Contains("gibt-es-nicht", a, StringComparison.Ordinal);
            Assert.Null(Katalogquelle.Lesen("", out string b));
            Assert.NotNull(b);
        }

        [Fact]
        public void Die_Zerlegung_einer_CSV_Zeile_kennt_Anfuehrungszeichen()
        {
            Assert.Equal(new[] { "a", "b", "c" }, Katalogquelle.Felder("a;b;c"));
            Assert.Equal(new[] { "a", "b;c", "d" }, Katalogquelle.Felder("a;\"b;c\";d"));
            Assert.Equal(new[] { "a", "", "c" }, Katalogquelle.Felder("a;;c"));
            Assert.Equal(new[] { "a\"b" }, Katalogquelle.Felder("\"a\"\"b\""));
            Assert.Equal(new[] { "a", "b" }, Katalogquelle.Felder(" a ; b "));
        }

        [Fact]
        public void Die_Testdatenbank_ist_eine_vollstaendige_Katalogquelle()
        {
            using var v = new Vorrichtung();
            string quelle = Path.Combine(v.Wurzel, "Referenzlaeufe", "Kenndaten_Test.sqlite");
            if (!File.Exists(quelle) || new FileInfo(quelle).Length < 100_000) return;   // kein LFS-Inhalt

            // Gegen eine KOPIE: Die Testdatenbank ist die Messlatte jedes Referenzlaufs.
            string kopie = Path.Combine(v.Arbeitsordner, "Kenndaten_Kopie.sqlite");
            File.Copy(quelle, kopie);

            Katalog k = Katalogquelle.Lesen(kopie, out string fehler);
            Assert.Null(fehler);
            Assert.NotNull(k);
            Assert.True(k.Arten.Count >= 5, "Die Testdatenbank fuehrt mindestens die fuenf abgeleiteten Arten.");
            Assert.True(k.Parameter.Anzahl >= 20);
            Assert.All(k.Arten, a => Assert.NotNull(a.Tagesgaenge));
            Assert.All(k.Arten, a => Assert.Equal(12, a.Monatsfaktoren.Length));
            Assert.True(k.Parameter.Enthaelt(ZapfParameter.KALTWASSER_MITTEL),
                "Ohne Kaltwasserparameter rechnet das Mengengeruest nicht.");

            // Die Quelle bleibt unberührt - keine WAL-Beidatei neben der Kopie.
            Assert.False(File.Exists(kopie + "-wal"), "Die unveraenderliche Verbindung darf kein WAL anlegen.");
            Assert.False(File.Exists(kopie + "-shm"));
        }

        [Fact]
        public void Die_unveraenderliche_Uri_maskiert_Sonderzeichen()
        {
            string uri = Sqlitehilfe.UnveraenderlichUri(Path.Combine(Path.GetTempPath(), "a b#c.sqlite"));
            Assert.StartsWith("file://", uri, StringComparison.Ordinal);
            Assert.EndsWith("?immutable=1", uri, StringComparison.Ordinal);
            Assert.Contains("%20", uri, StringComparison.Ordinal);
            Assert.Contains("%23", uri, StringComparison.Ordinal);
            Assert.DoesNotContain('\\', uri);
        }
    }
}
